using Blog.Exceptions;
using Blog.Models;
using Blog.Repositories.Interfaces;
using Blog.Services.Interfaces;
using Minio;
using Minio.DataModel.Args;

namespace Blog.Services
{
    public class PostService : IPostService
    {
        private readonly IMinioRepository _minioRepository;
        private readonly IPostRepository _postRepository;
        private readonly IIdempotencyKeysRepository _usedIdempotencyKeys;

        public PostService(IMinioRepository minioRepository, IPostRepository postRepository, IIdempotencyKeysRepository idempotencyKeysRepository)
        {
            _minioRepository = minioRepository;
            _postRepository = postRepository;
            _usedIdempotencyKeys = idempotencyKeysRepository;
        }

        public async Task<PostModel?> GetPostByIdAsync(Guid id)
        {
            return await _postRepository.GetPostByIdAsync(id);
        }

        public async Task<List<PostModel>> GetPostsByAuthorIdAsync(Guid authorId)
        {
            return await _postRepository.GetPostsByAuthorIdAsync(authorId);
        }

        public async Task<List<PostModel>> GetPublishedPostsAsync()
        {
            return await _postRepository.GetPublishedPostsAsync();
        }

        public async Task<PostModel> CreatePostAsync(string authorId, CreatePostRequest postRequest)
        {
            if (_usedIdempotencyKeys.Contains(postRequest.IdempotencyKey))
                throw new ConflictException("IdempotencyKey has already been used.");

            var newPost = new PostModel
            {
                PostId = Guid.NewGuid(),
                AuthorId = Guid.Parse(authorId),
                IdempotencyKey = postRequest.IdempotencyKey,
                Title = postRequest.Title,
                Content = postRequest.Content,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Status = "Draft"
            };

            await _postRepository.AddPostAsync(newPost);
            _usedIdempotencyKeys.Add(postRequest.IdempotencyKey);

            return newPost;
        }

        public async Task<PostModel?> UpdatePostAsync(Guid postId, string authorId, UpdatePost updatePost)
        {
            var post = await _postRepository.GetPostByIdAsync(postId);
            if (post == null)
                throw new NotFoundException("Post not found.");
            if (post.AuthorId.ToString() != authorId)
                throw new ForbiddenException("Access denied.");

            post.Title = updatePost.Title;
            post.Content = updatePost.Content;
            post.UpdatedAt = DateTime.UtcNow;

            await _postRepository.UpdatePostAsync(post);

            return post;
        }


        public async Task<bool> DeleteImageAsync(Guid postId, Guid imageId, string authorId)
        {
            var post = await _postRepository.GetPostByIdAsync(postId);

            if (post == null)
                throw new NotFoundException("Post not found.");
            if (post.AuthorId.ToString() != authorId)
                throw new ForbiddenException("Access denied.");

            var image = post.Images.FirstOrDefault(img => img.ImageId == imageId);
            if (image == null)
                throw new NotFoundException("Image not found.");

            var bucketName = "post-images";
            var objectName = $"{postId}/{imageId}";

            await _minioRepository.DeleteObjectAsync(bucketName, objectName);

            post.Images.Remove(image);

            return true;
        }

        public async Task<List<ImageModel>> AddImagesToPostAsync(Guid postId, string authorId, List<IFormFile> images)
        {
            var post = await _postRepository.GetPostByIdAsync(postId);
            if (post == null)
                throw new NotFoundException("Post not found.");
            if (post.AuthorId.ToString() != authorId)
                throw new ForbiddenException("Access denied.");

            var bucketName = "post-images";
            await _minioRepository.CreateBucketAsync(bucketName);

            var uploadedImages = new List<ImageModel>();

            foreach (var image in images)
            {
                if (image.Length == 0) continue;

                var id = Guid.NewGuid();
                var objectName = $"{postId}/{id}";

                await using var stream = image.OpenReadStream();
                await _minioRepository.UploadObjectAsync(bucketName, objectName, stream, image.Length, image.ContentType);

                var imageUrl = await _minioRepository.GetPresignedUrlAsync(bucketName, objectName, 3600);

                var newImage = new ImageModel
                {
                    ImageId = id,
                    PostId = postId,
                    ImageUrl = imageUrl,
                    CreatedAt = DateTime.UtcNow
                };

                post.Images.Add(newImage);
                uploadedImages.Add(newImage);
            }

            return uploadedImages;
        }

        public async Task<PostModel?> PublishPostAsync(Guid postId, string authorId, PublishPostRequest request)
        {
            var post = await _postRepository.GetPostByIdAsync(postId);
            if (post == null)
                throw new NotFoundException("Post not found.");
            if (post.AuthorId.ToString() != authorId)
                throw new ForbiddenException("Access denied.");

            post.Status = request.Status;
            post.UpdatedAt = DateTime.UtcNow;

            await _postRepository.UpdatePostAsync(post);

            return post;
        }

    }
}
