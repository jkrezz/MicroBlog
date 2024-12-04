using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Blog.Models;
using Blog.Services.Interfaces;
using Minio;
using Minio.DataModel.Args;

namespace Blog.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PostsController : ControllerBase
{
    private readonly IPostService _postService;

    public PostsController(IPostService postService)
    {
        _postService = postService;
    }

    [Authorize(Roles = "Author")]
    [HttpPost]
    public async Task<IActionResult> CreatePost([FromBody] CreatePostRequest postRequest)
    {
        var authorId = User.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value;
        var newPost = await _postService.CreatePostAsync(authorId, postRequest);
        return CreatedAtAction(nameof(GetPostById), new { id = newPost.PostId }, newPost);
    }

    [Authorize(Roles = "Author")]
    [HttpPost("{postId}/images")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> AddImagesToPost([FromRoute] Guid postId, [FromForm] List<IFormFile> images)
    {
        var authorId = User.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value;
        var uploadedImages = await _postService.AddImagesToPostAsync(postId, authorId, images);
        return Created("", new { UploadedImages = uploadedImages });
    }

    [Authorize(Roles = "Author")]
    [HttpPut("{postId}")]
    public async Task<IActionResult> EditPost([FromRoute] Guid postId, [FromBody] UpdatePost updatePost)
    {
        var authorId = User.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value;
        var updatedPost = await _postService.UpdatePostAsync(postId, authorId, updatePost);
        return Ok(new { Message = "Post successfully updated.", UpdatedPost = updatedPost });
    }

    [Authorize(Roles = "Author")]
    [HttpDelete("{postId}/images/{imageId}")]
    public async Task<IActionResult> DeleteImages([FromRoute] Guid postId, [FromRoute] Guid imageId)
    {
        var authorId = User.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value;
        var success = await _postService.DeleteImageAsync(postId, imageId, authorId);

        if (!success)
            return NotFound("Image or post not found, or access denied.");

        return Ok(new { Message = "Image successfully deleted." });
    }

    [Authorize(Roles = "Author")]
    [HttpPatch("{postId}/status")]
    public async Task<IActionResult> PublishPost([FromRoute] Guid postId, [FromBody] PublishPostRequest request)
    {
        var authorId = User.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value;
        var updatedPost = await _postService.PublishPostAsync(postId, authorId, request);
        return Ok(new { Message = "Post successfully published.", UpdatedPost = updatedPost });
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> GetPosts()
    {
        var userRole = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
        var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier).Value;

        if (userRole == "Author")
        {
            var posts = await _postService.GetPostsByAuthorIdAsync(Guid.Parse(userId));
            return Ok(new { Message = "List of posts for the author.", Posts = posts });
        }

        if (userRole == "Reader")
        {
            var posts = await _postService.GetPublishedPostsAsync();
            return Ok(new { Message = "List of published posts.", Posts = posts });
        }

        return Forbid("Access denied.");
    }

    [Authorize(Roles = "Author")]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetPostById([FromRoute] Guid id)
    {
        var post = await _postService.GetPostByIdAsync(id);

        if (post == null)
            return NotFound("Post not found.");

        return Ok(post);
    }
}