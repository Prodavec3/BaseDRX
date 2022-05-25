using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.OfficialDocument;

namespace Sungero.RecordManagement
{
  partial class InternalDocumentsReportServerHandlers
  {

    public virtual IQueryable<Sungero.Docflow.IInternalDocumentBase> GetInternalDocuments()
    {
      return Sungero.Docflow.InternalDocumentBases.GetAll()
        .Where(d => d.DocumentKind.DocumentFlow == Docflow.DocumentKind.DocumentFlow.Inner)
        .Where(d => d.RegistrationState == RegistrationState.Registered)
        .Where(d => Equals(d.DocumentRegister, InternalDocumentsReport.DocumentRegister))
        .Where(d => d.RegistrationDate >= InternalDocumentsReport.BeginDate)
        .Where(d => d.RegistrationDate <= InternalDocumentsReport.EndDate)
        .OrderBy(d => d.RegistrationDate)
        .ThenBy(d => d.Index);
    }
  }
}