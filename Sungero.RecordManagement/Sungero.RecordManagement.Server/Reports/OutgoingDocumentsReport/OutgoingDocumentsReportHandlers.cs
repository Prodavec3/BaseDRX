using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.RecordManagement
{
  partial class OutgoingDocumentsReportServerHandlers
  {

    public virtual IQueryable<Sungero.Docflow.IOutgoingDocumentBase> GetLetters()
    {
      return Sungero.Docflow.OutgoingDocumentBases.GetAll()
        .Where(l => l.DocumentKind.DocumentFlow == Docflow.DocumentKind.DocumentFlow.Outgoing)
        .Where(l => l.RegistrationState == Sungero.Docflow.OfficialDocument.RegistrationState.Registered)
        .Where(l => Equals(l.DocumentRegister, OutgoingDocumentsReport.DocumentRegister))
        .Where(l => l.RegistrationDate >= OutgoingDocumentsReport.BeginDate)
        .Where(l => l.RegistrationDate <= OutgoingDocumentsReport.EndDate)
        .OrderBy(l => l.RegistrationDate)
        .ThenBy(l => l.Index);
    }

  }

}