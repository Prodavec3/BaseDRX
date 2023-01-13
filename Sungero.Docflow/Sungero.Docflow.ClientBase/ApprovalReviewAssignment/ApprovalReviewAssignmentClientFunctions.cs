using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ApprovalReviewAssignment;

namespace Sungero.Docflow.Client
{
  partial class ApprovalReviewAssignmentFunctions
  {

    /// <summary>
    /// Установка ЭП.
    /// </summary>
    /// <param name="e">Параметр действия.</param>
    /// <param name="comment">Комментарий.</param>
    public void SetSignature(Sungero.Workflow.Client.ExecuteResultActionArgs e, string comment)
    {
      // Не подписывать, если это внесение результата рассмотрения.
      if (_obj.IsResultSubmission == true)
        return;
      
      var needStrongSign = _obj.Stage.NeedStrongSign ?? false;
      
      var document = _obj.DocumentGroup.OfficialDocuments.FirstOrDefault();
      var addenda = _obj.AddendaGroup.OfficialDocuments.ToList();
      var addendaToElectronic = _obj.AddendaGroup.OfficialDocuments.ToList<Sungero.Content.IElectronicDocument>();
      var performer = Company.Employees.As(_obj.Performer);
      var canSignByEmployee = Functions.OfficialDocument.Remote.CanSignByEmployee(document, Company.Employees.Current);
      
      // Подписать утверждающей подписью, если нет прав, то согласующей.
      if (document.AccessRights.CanApprove() && canSignByEmployee)
      {
        // Для утверждения необходимо, чтобы документ не был заблокирован.
        var lockInfo = Functions.OfficialDocument.GetDocumentLockInfo(document);
        if (lockInfo != null && lockInfo.IsLocked)
        {
          e.AddError(Sungero.Docflow.ApprovalReviewAssignments.Resources.CanNotSetSignatureFormat(lockInfo.OwnerName, lockInfo.LockTime));
          return;
        }
        Functions.Module.ApproveDocument(document, addenda, performer, needStrongSign, comment, e);
      }
      else
        Functions.Module.EndorseDocument(document, addendaToElectronic, performer, true, needStrongSign, comment, e);
    }
  }
}