using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow;
using Sungero.Domain.Shared;
using Sungero.Metadata;
using Sungero.Workflow;

namespace Sungero.SmartProcessing.Server
{
  public class ModuleJobs
  {
    
    /// <summary>
    /// Фоновый процесс для удаления пакетов бинарных образов документов, которые отправлены на верификацию.
    /// </summary>
    public virtual void DeleteBlobPackages()
    {
      // Удаление BlobPackage со статусом Processed.
      var processedBlobPackages = BlobPackages.GetAll().Where(x => x.ProcessState == SmartProcessing.BlobPackage.ProcessState.Processed);
      foreach (var blobPackage in processedBlobPackages)
      {
        var blobs = blobPackage.Blobs.Select(x => x.Blob);
        var mailBodyBlob = blobPackage.MailBodyBlob;
        BlobPackages.Delete(blobPackage);
        foreach (var blob in blobs)
          Blobs.Delete(blob);
        
        if (mailBodyBlob != null)
          Blobs.Delete(mailBodyBlob);
      }
    }

  }
}