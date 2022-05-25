using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ApprovalSigningAssignment;

namespace Sungero.Docflow.Client
{
  partial class ApprovalSigningAssignmentFunctions
  {
    /// <summary>
    /// Показывать сводку по документу.
    /// </summary>
    /// <returns>True, если в задании нужно показывать сводку по документу.</returns>
    [Public]
    public virtual bool NeedViewDocumentSummary()
    {
      var document = _obj.DocumentGroup.OfficialDocuments.FirstOrDefault();
      if (document == null)
        return false;
      
      return Docflow.PublicFunctions.OfficialDocument.NeedViewDocumentSummary(document);
    }
    
     /// <summary>
    /// Подписать документ.
    /// </summary>
    /// <param name="needStrongSign">Требуется квалифицированная электронная подпись.</param>
    /// <param name="eventArgs">Аргумент обработчика вызова.</param>
    public virtual void ApproveDocument(bool needStrongSign, Sungero.Domain.Client.ExecuteActionArgs eventArgs)
    {
      var document = _obj.DocumentGroup.OfficialDocuments.Single();
      var addenda = _obj.AddendaGroup.OfficialDocuments.ToList();
      var performer = Company.Employees.As(_obj.Performer);
      var comment = string.IsNullOrWhiteSpace(_obj.ActiveText) ? string.Empty : _obj.ActiveText;
      
      Functions.Module.ApproveDocument(document, addenda, performer, needStrongSign, comment, eventArgs);
    }
  }
}