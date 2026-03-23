using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhotoWebappAPI.Data;
using PhotoWebappAPI.DTOs.Post;
using PhotoWebappAPI.Models;
using PhotoWebappAPI.Services.Interfaces;
using System.Security.Claims;


namespace PhotoWebappAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Photographer")] // Khóa bảo mật: CHỈ THỢ mới được đăng bài!
    public class PostsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly IPhotoService _photoService;

        public PostsController(ApplicationDbContext context, UserManager<AppUser> userManager, IPhotoService photoService)
        {
            _context = context;
            _userManager = userManager;
            _photoService = photoService;
        }

        [HttpPost]
        public async Task<IActionResult> CreatePost([FromForm] CreatePostDto dto)
        {
            // 1. Nhận diện thợ nào đang thao tác
            var email = User.FindFirstValue(ClaimTypes.Email);
            if (email == null) return Unauthorized("Token không hợp lệ.");

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return NotFound("Không tìm thấy thợ chụp ảnh.");

            // 2. Tạo phần CHỮ của bài đăng trước
            var newPost = new Post
            {
                Title = dto.Title,
                Description = dto.Description,
                PhotographerId = user.Id, // Đã map đúng cột PhotographerId của bạn
                CreatedAt = DateTime.UtcNow
            };

            _context.Posts.Add(newPost);
            await _context.SaveChangesAsync(); // Lưu xuống DB để lấy cái Id của bài đăng

            // 3. Xử lý phần ẢNH: Lặp qua từng file để đẩy lên mây
            if (dto.Images != null && dto.Images.Count > 0)
            {
                foreach (var file in dto.Images)
                {
                    var uploadResult = await _photoService.AddPhotoAsync(file);

                    if (uploadResult.Error == null) // Nếu up thành công
                    {
                        var photo = new Photo
                        {
                            Url = uploadResult.SecureUrl.ToString(), // Chỉ lưu mỗi Url như Model của bạn
                            PostId = newPost.Id
                        };
                        _context.Photos.Add(photo);
                    }
                }
                await _context.SaveChangesAsync(); // Lưu một loạt link ảnh vào DB
            }

            return Ok(new
            {
                message = "Đăng bài Portfolio thành công rực rỡ!",
                postId = newPost.Id
            });
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllPosts([FromQuery] PostFilterDto filter)
        {
            // 1. Khởi tạo query từ database nhưng CHƯA thực thi ngay (IQueryable)
            var query = _context.Posts
                .Include(p => p.Photographer)
                .Include(p => p.Photos)
                .AsQueryable();

            // 2. Lọc theo từ khóa (Tiêu đề hoặc Mô tả)
            if (!string.IsNullOrEmpty(filter.SearchTerm))
            {
                query = query.Where(p => p.Title.Contains(filter.SearchTerm)
                                      || (p.Description != null && p.Description.Contains(filter.SearchTerm)));
            }

            // 3. Lọc theo tên Thợ chụp ảnh
            if (!string.IsNullOrEmpty(filter.PhotographerName))
            {
                query = query.Where(p => p.Photographer.FullName.Contains(filter.PhotographerName));
            }

            // 4. Sắp xếp
            query = filter.SortBy == "oldest"
                ? query.OrderBy(p => p.CreatedAt)
                : query.OrderByDescending(p => p.CreatedAt);

            // 5. Lúc này mới thực sự chạy xuống SQL Server lấy dữ liệu
            var posts = await query.Select(p => new PostResponseDto
            {
                Id = p.Id,
                Title = p.Title,
                Description = p.Description,
                CreatedAt = p.CreatedAt,
                PhotographerName = p.Photographer.FullName,
                Photos = p.Photos.Select(img => new PhotoDto
                {
                    Id = img.Id,
                    Url = img.Url
                }).ToList()
            }).ToListAsync();

            return Ok(posts);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Photographer")]
        public async Task<IActionResult> UpdatePost(int id, UpdatePostDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var post = await _context.Posts.FindAsync(id);

            if (post == null) return NotFound("Không thấy bài đăng.");
            if (post.PhotographerId != userId) return Forbid("Không có quyền sửa bài người khác.");

            post.Title = dto.Title;
            post.Description = dto.Description;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Cập nhật bài đăng thành công!" });
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Photographer")] // Chỉ thợ mới được xóa bài của mình
        public async Task<IActionResult> DeletePost(int id)
        {
            // 1. Nhận diện thợ nào đang thao tác
            var email = User.FindFirstValue(ClaimTypes.Email);
            if (email == null) return Unauthorized();

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return NotFound();

            // 2. Tìm bài đăng, bao gồm cả danh sách ảnh của nó
            var post = await _context.Posts
                .Include(p => p.Photos)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (post == null) return NotFound("Không tìm thấy bài đăng.");

            // 3. Bảo mật: Kiểm tra xem bài đăng này có phải của thợ này không
            if (post.PhotographerId != user.Id)
                return Forbid("Bạn không có quyền xóa bài đăng của người khác.");

            // 4. Xử lý xóa ảnh trên Cloudinary (Quan trọng nhất!)
            if (post.Photos.Any())
            {
                foreach (var photo in post.Photos)
                {
                    // Kiểm tra xem ảnh có PublicId không
                    if (!string.IsNullOrEmpty(photo.PublicId))
                    {
                        // Gọi sang Cloudinary để xóa file ảnh thật trên mây
                        var deleteResult = await _photoService.DeletePhotoAsync(photo.PublicId);

                        // Nếu Cloudinary báo lỗi (ví dụ: sai ID), chúng ta log lại hoặc xử lý
                        if (deleteResult.Error != null)
                        {
                            // Tùy chọn: Log lỗi hoặc bỏ qua để tiếp tục xóa SQL
                            // return BadRequest($"Lỗi khi xóa ảnh trên Cloudinary: {deleteResult.Error.Message}");
                        }
                    }
                }
            }

            // 5. Cuối cùng, xóa bài đăng trong Database SQL Server
            // Do bạn thiết kế Cascade Delete (Post -> Photos), nên khi xóa Post, SQL tự xóa Photos luôn
            _context.Posts.Remove(post);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã xóa bài đăng và toàn bộ ảnh trên mây thành công!" });
        }
    }
}
