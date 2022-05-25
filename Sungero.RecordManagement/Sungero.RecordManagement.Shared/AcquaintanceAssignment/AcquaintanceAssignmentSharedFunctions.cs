using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Content;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.AcquaintanceAssignment;

namespace Sungero.RecordManagement.Shared
{
  partial class AcquaintanceAssignmentFunctions
  {
    /// <summary>
    /// Сохранить номер версии и хеш документа в задании.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="isMainDocument">Признак главного документа.</param>
    /// <param name="mainDocumentTaskVersionNumber">Номер версии основного документа в задаче.</param>
    public void StoreAcquaintanceVersion(IElectronicDocument document, bool isMainDocument, int? mainDocumentTaskVersionNumber)
    {
      var acqTaskVersion = document.LastVersion;
      if (mainDocumentTaskVersionNumber != null)
        acqTaskVersion = document.Versions.FirstOrDefault(v => v.Number == mainDocumentTaskVersionNumber);
      
      var mainDocumentVersion = _obj.AcquaintanceVersions.AddNew();
      mainDocumentVersion.IsMainDocument = isMainDocument;
      mainDocumentVersion.DocumentId = document.Id;
      if (acqTaskVersion != null)
      {
        mainDocumentVersion.Number = acqTaskVersion.Number;
        mainDocumentVersion.Hash = acqTaskVersion.Body.Hash;
      }
      else
      {
        mainDocumentVersion.Number = 0;
        mainDocumentVersion.Hash = null;
      }
    }
  }
}