using System.ComponentModel.DataAnnotations;
using LibrarySystem.Application.Catalog;
using Microsoft.AspNetCore.Hosting;

namespace LibrarySystem.Infrastructure.Catalog;

public sealed class LocalBookCoverStorage(IWebHostEnvironment environment) : IBookCoverStorage
{
    private const int MaximumFileSize = 5 * 1024 * 1024;

    private static readonly Dictionary<string, string> AllowedContentTypes =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["image/jpeg"] = ".jpg",
            ["image/png"] = ".png",
            ["image/webp"] = ".webp"
        };

    public async Task<string> SaveAsync(
        Stream content,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        if (!AllowedContentTypes.TryGetValue(contentType, out var expectedExtension))
        {
            throw new ValidationException("Only JPEG, PNG, and WebP cover images are supported.");
        }

        var suppliedExtension = Path.GetExtension(fileName);
        if (!string.Equals(suppliedExtension, expectedExtension, StringComparison.OrdinalIgnoreCase) &&
            !(contentType.Equals("image/jpeg", StringComparison.OrdinalIgnoreCase) &&
              suppliedExtension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase)))
        {
            throw new ValidationException("The cover image extension does not match its content type.");
        }

        var relativeDirectory = Path.Combine("uploads", "book-covers");
        var physicalDirectory = Path.Combine(environment.WebRootPath, relativeDirectory);
        Directory.CreateDirectory(physicalDirectory);

        var storedFileName = $"{Guid.NewGuid():N}{expectedExtension}";
        var physicalPath = Path.Combine(physicalDirectory, storedFileName);

        await using var destination = new FileStream(
            physicalPath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            81920,
            useAsync: true);

        var buffer = new byte[81920];
        var totalBytes = 0;
        try
        {
            int bytesRead;
            while ((bytesRead = await content.ReadAsync(buffer, cancellationToken)) > 0)
            {
                totalBytes += bytesRead;
                if (totalBytes > MaximumFileSize)
                {
                    throw new ValidationException("The cover image cannot exceed 5 MB.");
                }

                await destination.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
            }
        }
        catch
        {
            await destination.DisposeAsync();
            File.Delete(physicalPath);
            throw;
        }

        if (totalBytes == 0)
        {
            await destination.DisposeAsync();
            File.Delete(physicalPath);
            throw new ValidationException("The cover image is empty.");
        }

        return $"/{relativeDirectory.Replace('\\', '/')}/{storedFileName}";
    }

    public Task DeleteAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var allowedRoot = Path.GetFullPath(Path.Combine(environment.WebRootPath, "uploads", "book-covers"));
        var candidatePath = Path.GetFullPath(Path.Combine(
            environment.WebRootPath,
            relativePath.TrimStart('/', '\\')));

        if (!candidatePath.StartsWith(allowedRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            throw new ValidationException("The cover image path is invalid.");
        }

        if (File.Exists(candidatePath))
        {
            File.Delete(candidatePath);
        }

        return Task.CompletedTask;
    }
}
