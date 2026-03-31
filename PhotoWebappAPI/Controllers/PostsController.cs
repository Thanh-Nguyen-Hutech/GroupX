using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhotoWebappAPI.Data;
using PhotoWebappAPI.DTOs.Post;
using PhotoWebappAPI.Models;
using PhotoWebappAPI.Models.PhotoWebappAPI.Models;
using PhotoWebappAPI.Services.Interfaces;
using System.Security.Claims;

namespace PhotoWebappAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
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

        // CHỈ THỢ MỚI ĐƯỢC ĐĂNG BÀI
        [HttpPost("multiple")]
        [Authorize(Roles = "Photographer")]
        public async Task<IActionResult> CreatePost([FromForm] CreatePostDto dto)
        {
            var email = User.FindFirstValue(ClaimTypes.Email);
            if (email == null) return Unauthorized("Token không hợp lệ.");

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return NotFound("Không tìm thấy thợ chụp ảnh.");

            var newPost = new Post
            {
                Title = dto.Title,
                Description = dto.Description,
                PhotographerId = user.Id,
                CreatedAt = DateTime.UtcNow
            };

            _context.Posts.Add(newPost);
            await _context.SaveChangesAsync();

            if (dto.Images != null && dto.Images.Count > 0)
            {
                foreach (var file in dto.Images)
                {
                    var uploadResult = await _photoService.AddPhotoAsync(file);
                    if (uploadResult.Error == null)
                    {
                        var photo = new Photo
                        {
                            Url = uploadResult.SecureUrl.ToString(),
                            PostId = newPost.Id
                        };
                        _context.Photos.Add(photo);
                    }
                }
                await _context.SaveChangesAsync();
            }

            return Ok(new { message = "Đăng bài Portfolio thành công rực rỡ!", postId = newPost.Id });
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllPosts([FromQuery] PostFilterDto filter)
        {
            var query = _context.Posts
                .Include(p => p.Photographer)
                .Include(p => p.Photos)
                .Include(p => p.Likes)
                .Include(p => p.Comments)
                .AsQueryable();

            if (!string.IsNullOrEmpty(filter.SearchTerm))
            {
                query = query.Where(p => p.Title.Contains(filter.SearchTerm)
                                      || (p.Description != null && p.Description.Contains(filter.SearchTerm)));
            }

            if (!string.IsNullOrEmpty(filter.PhotographerName))
            {
                query = query.Where(p => p.Photographer.FullName.Contains(filter.PhotographerName));
            }

            if (!string.IsNullOrEmpty(filter.PhotographerId))
            {
                query = query.Where(p => p.PhotographerId == filter.PhotographerId);
            }

            query = filter.SortBy == "oldest"
                ? query.OrderBy(p => p.CreatedAt)
                : query.OrderByDescending(p => p.CreatedAt);

            var posts = await query.Select(p => new PostResponseDto
            {
                Id = p.Id,
                Title = p.Title,
                Description = p.Description,
                CreatedAt = p.CreatedAt,
                PhotographerId = p.PhotographerId, // ✅ Map ID
                PhotographerName = p.Photographer.FullName,
                PhotographerAvatar = p.Photographer.Avatar,
                Photos = p.Photos.Select(img => new PhotoDto { Id = img.Id, Url = img.Url }).ToList(),
                LikesCount = p.Likes.Count,
                Comments = p.Comments.Select(c => new CommentResponseDto { Id = c.Id, Author = "User", Text = c.Text }).ToList()
            }).ToListAsync();

            return Ok(posts);
        }


        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPostById(int id)
        {
            var post = await _context.Posts
                .Include(p => p.Photographer)
                .Include(p => p.Photos)
                .Include(p => p.Likes)
                .Include(p => p.Comments)
                .ThenInclude(c => c.User)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (post == null) return NotFound("Không thấy bài đăng");

            var response = new PostResponseDto
            {
                Id = post.Id,
                Title = post.Title,
                Description = post.Description,
                CreatedAt = post.CreatedAt,
                PhotographerName = post.Photographer.FullName,
                Photos = post.Photos.Select(img => new PhotoDto { Id = img.Id, Url = img.Url }).ToList(),
                LikesCount = post.Likes.Count,
                Comments = post.Comments.Select(c => new CommentResponseDto
                {
                    Id = c.Id,
                    Author = c.User.FullName ?? "Người dùng",
                    Text = c.Text
                }).ToList()
            };

            return Ok(response);
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
        [Authorize(Roles = "Photographer")]
        public async Task<IActionResult> DeletePost(int id)
        {
            var email = User.FindFirstValue(ClaimTypes.Email);
            var user = await _userManager.FindByEmailAsync(email);

            // 1. Lấy bài đăng bao gồm cả Photos, Likes và Comments
            var post = await _context.Posts
                .Include(p => p.Photos)
                .Include(p => p.Likes)      // <--- Thêm Include này
                .Include(p => p.Comments)   // <--- Thêm Include này
                .FirstOrDefaultAsync(p => p.Id == id);

            if (post == null) return NotFound();
            if (post.PhotographerId != user.Id) return Forbid();

            // 2. Xóa dữ liệu liên quan trong Database

            // Xóa ảnh (cả file vật lý và bản ghi DB)
            if (post.Photos != null && post.Photos.Any())
            {
                foreach (var photo in post.Photos)
                {
                    if (!string.IsNullOrEmpty(photo.PublicId))
                    {
                        await _photoService.DeletePhotoAsync(photo.PublicId);
                    }
                }
                _context.Photos.RemoveRange(post.Photos);
            }

            // Xóa Likes
            if (post.Likes != null && post.Likes.Any())
            {
                _context.Likes.RemoveRange(post.Likes);
            }

            // Xóa Comments
            if (post.Comments != null && post.Comments.Any())
            {
                _context.Comments.RemoveRange(post.Comments);
            }

            // 3. Sau khi đã "dọn dẹp" sạch sẽ các bảng liên quan, mới xóa Post
            _context.Posts.Remove(post);

            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã xóa bài đăng thành công!" });
        }

        // ✅ CẢ KHÁCH VÀ THỢ ĐỀU LIKE ĐƯỢC
        [HttpPost("{id}/like")]
        [Authorize]
        public async Task<IActionResult> ToggleLike(int id)
        {
            var email = User.FindFirstValue(ClaimTypes.Email);
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return Unauthorized();

            var post = await _context.Posts.FindAsync(id);
            if (post == null) return NotFound("Không tìm thấy bài đăng.");

            var existingLike = await _context.Likes
                .FirstOrDefaultAsync(l => l.PostId == id && l.UserId == user.Id);

            if (existingLike == null)
            {
                _context.Likes.Add(new Like { PostId = id, UserId = user.Id });
                await _context.SaveChangesAsync();
                return Ok(new { message = "Đã thả tim!" });
            }
            else
            {
                _context.Likes.Remove(existingLike);
                await _context.SaveChangesAsync();
                return Ok(new { message = "Đã bỏ tim!" });
            }
        }

        // ✅ CẢ KHÁCH VÀ THỢ ĐỀU CMT ĐƯỢC
        [HttpPost("{id}/comment")]
        [Authorize]
        public async Task<IActionResult> AddComment(int id, [FromBody] CommentDto dto)
        {
            var email = User.FindFirstValue(ClaimTypes.Email);
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return Unauthorized();

            var post = await _context.Posts.FindAsync(id);
            if (post == null) return NotFound("Không tìm thấy bài đăng.");

            var comment = new Comment { Text = dto.Text, PostId = id, UserId = user.Id };
            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            return Ok(new { id = comment.Id, author = user.FullName, text = comment.Text });
        }

        [HttpGet("my-posts")]
        [Authorize(Roles = "Photographer")]
        public async Task<IActionResult> GetMyPosts()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier); // Lấy ID chuẩn từ Token
            var query = _context.Posts
                .Include(p => p.Photos)
                .Include(p => p.Likes)
                .Where(p => p.PhotographerId == userId)
                .OrderByDescending(p => p.CreatedAt);

            var posts = await query.Select(p => new
            {
                Id = p.Id,
                Title = p.Title,
                CreatedAt = p.CreatedAt,
                Photos = p.Photos.Select(img => new { Url = img.Url }).ToList(),
                LikesCount = p.Likes.Count
            }).ToListAsync();

            return Ok(posts);
        }
    }
}