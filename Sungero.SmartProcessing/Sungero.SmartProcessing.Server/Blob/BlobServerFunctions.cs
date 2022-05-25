using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.SmartProcessing.Blob;

namespace Sungero.SmartProcessing.Server
{
  partial class BlobFunctions
  {
    
    /// <summary>
    /// Создать блоб с телом документа и дополнительной информацией.
    /// </summary>
    /// <returns>Блоб.</returns>
    [Remote, Public]
    public static IBlob CreateBlob()
    {
      return Blobs.Create();
    }
    
    /// <summary>
    /// Создать бинарный образ документа из бинарного образа документа DCS.
    /// </summary>
    /// <param name="dcsBlob">Бинарный образ документа DCS (используется для обработки на клиенте).</param>
    /// <returns>Бинарный образ документа.</returns>
    [Public]
    public static IBlob CreateBlob(Structures.Module.IDcsBlob dcsBlob)
    {
      var blob = CreateBlob();
      
      blob.FilePath = dcsBlob.FilePath;
      blob.OriginalFileName = dcsBlob.OriginalFileName;
      blob.ArioResultJson = dcsBlob.ArioResultJson;
      blob.ArioTaskStatus = SmartProcessing.Blob.ArioTaskStatus.InProcess;
      if (dcsBlob.Body != null)
      {
        using (var dcsBlobBody = new MemoryStream(dcsBlob.Body))
          blob.Body.Write(dcsBlobBody);
      }
      blob.PageCount = dcsBlob.PageCount;
      blob.Created = dcsBlob.Created.FromUtcTime();
      blob.Modified = dcsBlob.Modified.FromUtcTime();
      
      // Для хранения в справочнике long свойства оно преобразовывается в строку и обратно.
      blob.FileSize = dcsBlob.FileSize.ToString();
      
      blob.Save();
      return blob;
    }
    
    /// <summary>
    /// Создать бинарный образ документа DCS на основе бинарного образа документа.
    /// </summary>
    /// <returns>Бинарный образ документа DCS (используется для обработки на клиенте).</returns>
    [Public]
    public virtual Structures.Module.IDcsBlob CreateDcsBlob()
    {
      var dcsBlob = Structures.Module.DcsBlob.Create();
      
      dcsBlob.FilePath = _obj.FilePath;
      dcsBlob.OriginalFileName = _obj.OriginalFileName;
      dcsBlob.ArioResultJson = _obj.ArioResultJson;
      
      dcsBlob.PageCount = _obj.PageCount;
      dcsBlob.Created = _obj.Created;
      dcsBlob.Modified = _obj.Modified;
      
      // Для хранения в справочнике long свойства оно преобразовывается в строку и обратно.
      long fileSize = 0;
      long.TryParse(_obj.FileSize, out fileSize);
      dcsBlob.FileSize = fileSize;
      
      if (_obj.Body != null)
      {
        using (var blobBody = _obj.Body.Read())
        {
          var bufferLen = (int)blobBody.Length;
          var buffer = new byte[bufferLen];
          blobBody.Read(buffer, 0, bufferLen);
          dcsBlob.Body = buffer;
        }
      }
      
      return dcsBlob;
    }
  }
}