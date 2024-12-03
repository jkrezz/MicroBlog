using Blog.Models;
using Blog.Services.Interfaces;
using Minio;
using Minio.DataModel.Args;

namespace Blog.Services
{
    public class PostService : IPostService
    {
        private readonly IMinioClient _minioClient;
        private readonly List<PostModel> _posts;
        private readonly HashSet<string> _usedIdempotencyKeys;

        public PostService(IMinioClient minioClient)
        {
            _minioClient = minioClient;
            _posts = new List<PostModel>();
            _usedIdempotencyKeys = new HashSet<string>();
        }

        public async Task<PostModel?> GetPostByIdAsync(Guid id)
        {
            return _posts.FirstOrDefault(p => p.PostId == id);
        }

        public async Task<List<PostModel>> GetPostsByAuthorIdAsync(Guid authorId)
        {
            return _posts.Where(p => p.AuthorId == authorId).ToList();
        }

        public async Task<List<PostModel>> GetPublishedPostsAsync()
        {
            return _posts.Where(p => p.Status == "Published").ToList();
        }

        public async Task<PostModel> CreatePostAsync(string authorId, CreatePostRequest postRequest)
        {
            if (_usedIdempotencyKeys.Contains(postRequest.IdempotencyKey))
                throw new InvalidOperationException("IdempotencyKey has already been used.");

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

            _posts.Add(newPost);
            _usedIdempotencyKeys.Add(postRequest.IdempotencyKey);

            return newPost;
        }

        public async Task<PostModel?> UpdatePostAsync(Guid postId, string authorId, UpdatePost updatePost)
        {
            var post = _posts.FirstOrDefault(p => p.PostId == postId);
            if (post == null || post.AuthorId.ToString() != authorId)
                return null;

            post.Title = updatePost.Title;
            post.Content = updatePost.Content;
            post.UpdatedAt = DateTime.UtcNow;

            return post;
        }

        public async Task<bool> DeleteImageAsync(Guid postId, Guid imageId, string authorId)
        {
            var post = _posts.FirstOrDefault(p => p.PostId == postId);
            if (post == null || post.AuthorId.ToString() != authorId)
                return false;

            var image = post.Images.FirstOrDefault(img => img.ImageId == imageId);
            if (image == null)
                return false;

            var bucketName = "post-images";
            var objectName = $"{postId}/{imageId}";

            await _minioClient.RemoveObjectAsync(new RemoveObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName));

            post.Images.Remove(image);

            return true;
        }

        public async Task<List<ImageModel>> AddImagesToPostAsync(Guid postId, string authorId, List<IFormFile> images)
        {
            var post = _posts.FirstOrDefault(p => p.PostId == postId);
            if (post == null || post.AuthorId.ToString() != authorId)
                throw new UnauthorizedAccessException("Access denied.");

            var bucketName = "post-images";
            var uploadedImages = new List<ImageModel>();

            if (!await _minioClient.BucketExistsAsync(new BucketExistsArgs().WithBucket(bucketName)))
            {
                await _minioClient.MakeBucketAsync(new MakeBucketArgs().WithBucket(bucketName));
            }

            foreach (var image in images)
            {
                if (image.Length == 0) continue;

                var id = Guid.NewGuid();
                var objectName = $"{postId}/{id}";

                using var stream = image.OpenReadStream();
                await _minioClient.PutObjectAsync(new PutObjectArgs()
                    .WithBucket(bucketName)
                    .WithObject(objectName)
                    .WithStreamData(stream)
                    .WithObjectSize(image.Length)
                    .WithContentType(image.ContentType));

                var imageUrl = await _minioClient.PresignedGetObjectAsync(new PresignedGetObjectArgs()
                    .WithBucket(bucketName)
                    .WithObject(objectName)
                    .WithExpiry(3600));

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
            var post = _posts.FirstOrDefault(p => p.PostId == postId);
            if (post == null || post.AuthorId.ToString() != authorId)
                return null;

            post.Status = request.Status;
            post.UpdatedAt = DateTime.UtcNow;

            return post;
        }
    }
}
