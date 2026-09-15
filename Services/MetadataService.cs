using Formify.Api.Data;
using Formify.Api.Exceptions;
using Formify.Api.Models;
using interfaces;
using Microsoft.EntityFrameworkCore;

namespace Formify.Api.Services;

public class MetaDataService : IMetaDataService
{
    private readonly AppDbContext _db;

    public MetaDataService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<AppFileMetadata> GetMetadataAsync(Guid fileId, string key, CancellationToken ct = default)
    {
        try
        {
            AppFileMetadata? appFileMetadata = await _db.AppFileMetadata
            .Where(m => m.FileId == fileId && m.Key == key)
            .FirstOrDefaultAsync();

            if (appFileMetadata == null)
            {
                throw new NotFoundException($"metadata with id {fileId} found");
            }

            return appFileMetadata;

        }
        catch (Exception)
        {

            throw;
        }
    }

    public async Task UpdateMetadataAsync(Guid fileId, Dictionary<string, string> metadata)
    {
        try
        {
            foreach (KeyValuePair<string, string> keyValuePair in metadata)
            {
                AppFileMetadata appFileMetadata = await this.GetMetadataAsync(fileId, keyValuePair.Key);
                appFileMetadata.Key = keyValuePair.Key;
                appFileMetadata.Value = keyValuePair.Value;
                await _db.SaveChangesAsync();
            }
        }
        catch (Exception)
        {

            throw;
        }
    }
}