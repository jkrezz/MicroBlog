using Blog.Repositories.Interfaces;
using Minio;
using Minio.DataModel.Args;

namespace Blog.Repositories;

public class MinioRepository : IMinioRepository
{
    private readonly IMinioClient _minioClient;

    public MinioRepository(IMinioClient minioClient)
    {
        _minioClient = minioClient;
    }

    public async Task<bool> BucketExistsAsync(string bucketName)
    {
        return await _minioClient.BucketExistsAsync(new BucketExistsArgs().WithBucket(bucketName));
    }

    public async Task CreateBucketAsync(string bucketName)
    {
        if (!await BucketExistsAsync(bucketName))
        {
            await _minioClient.MakeBucketAsync(new MakeBucketArgs().WithBucket(bucketName));
        }
    }

    public async Task UploadObjectAsync(string bucketName, string objectName, Stream data, long size, string contentType)
    {
        await _minioClient.PutObjectAsync(new PutObjectArgs()
            .WithBucket(bucketName)
            .WithObject(objectName)
            .WithStreamData(data)
            .WithObjectSize(size)
            .WithContentType(contentType));
    }

    public async Task<string> GetPresignedUrlAsync(string bucketName, string objectName, int expiryInSeconds)
    {
        return await _minioClient.PresignedGetObjectAsync(new PresignedGetObjectArgs()
            .WithBucket(bucketName)
            .WithObject(objectName)
            .WithExpiry(expiryInSeconds));
    }

    public async Task DeleteObjectAsync(string bucketName, string objectName)
    {
        await _minioClient.RemoveObjectAsync(new RemoveObjectArgs()
            .WithBucket(bucketName)
            .WithObject(objectName));
    }
}