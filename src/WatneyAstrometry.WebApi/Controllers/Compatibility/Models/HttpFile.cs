// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

#pragma warning disable CS1591
namespace WatneyAstrometry.WebApi.Controllers.Compatibility.Models;

/// <summary>
/// A wrapper class for a byte array -> IFormFile.
/// </summary>
public class HttpFile : IFormFile
{
    private readonly byte[] _data;

    public HttpFile(byte[] data)
    {
        _data = data;
    }

    public Stream OpenReadStream()
    {
        return new MemoryStream(_data);
    }

    public void CopyTo(Stream target)
    {
        target.Write(_data);
    }

    public async Task CopyToAsync(Stream target, CancellationToken cancellationToken = new CancellationToken())
    {
        await target.WriteAsync(_data, cancellationToken);
    }

    public string ContentType { get; set; }
    public string ContentDisposition { get; set; }
    public IHeaderDictionary Headers { get; set; }
    public long Length => _data.Length;
    public string Name { get; set; }
    public string FileName { get; set; }
}