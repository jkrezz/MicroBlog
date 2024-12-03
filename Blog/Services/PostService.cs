using Blog.Models;
using Blog.Repositories.Interfaces;
using Blog.Services.Interfaces;
using Minio;
using Minio.DataModel.Args;

namespace Blog.Services
{
    public class PostService : IPostService
    {
        private readonly IMinioClient _minioClient;
        private readonly IPostRepository _postRepository;
        private readonly IIdempotencyKeysRepository _usedIdempotencyKeys;

        public PostService(IMinioClient minioClient, IPostRepository postRepository, IIdempotencyKeysRepository idempotencyKeysRepository)
        {
            _minioClient = minioClient;
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

            await _postRepository.AddPostAsync(newPost);
            _usedIdempotencyKeys.Add(postRequest.IdempotencyKey);

            return newPost;
        }

        public async Task<PostModel?> UpdatePostAsync(Guid postId, string authorId, UpdatePost updatePost)
        {
            var post = await _postRepository.GetPostByIdAsync(postId);
            if (post == null || post.AuthorId.ToString() != authorId)
                return null;

            post.Title = updatePost.Title;
            post.Content = updatePost.Content;
            post.UpdatedAt = DateTime.UtcNow;

            await _postRepository.UpdatePostAsync(post);

            return post;
        }


        public async Task<bool> DeleteImageAsync(Guid postId, Guid imageId, string authorId)
        {
            var post = await _postRepository.GetPostByIdAsync(postId);
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
            var post = await _postRepository.GetPostByIdAsync(postId);
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
            var post = await _postRepository.GetPostByIdAsync(postId);
            if (post == null || post.AuthorId.ToString() != authorId)
                return null;

            post.Status = request.Status;
            post.UpdatedAt = DateTime.UtcNow;

            await _postRepository.UpdatePostAsync(post);

            return post;
        }

    }
}
