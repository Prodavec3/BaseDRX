using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ApprovalSigningAssignment;
using Sungero.Domain.Shared;

namespace Sungero.Docflow.Server
{
  partial class ApprovalSigningAssignmentFunctions
  {
    #region Контроль состояния
    
    /// <summary>
    /// Построить регламент.
    /// </summary>
    /// <returns>Регламент.</returns>
    [Remote(IsPure = true)]
    public Sungero.Core.StateView GetStagesStateView()
    {
      return PublicFunctions.ApprovalRuleBase.GetStagesStateView(_obj);
    }
    
    /// <summary>
    /// Построить сводку по документу.
    /// </summary>
    /// <returns>Сводка по документу.</returns>
    [Remote(IsPure = true)]
    public StateView GetDocumentSummary()
    {
      var document = _obj.DocumentGroup.OfficialDocuments.FirstOrDefault();
      return Docflow.PublicFunctions.Module.GetDocumentSummary(document);
    }
    
    #endregion
    
    /// <summary>
    /// Получить модель контрола состояния листа согласования.
    /// </summary>
    /// <returns>Модель контрола состояния листа согласования.</returns>
    [Remote(IsPure = true)]
    public StateView GetApprovalListState()
    {
      var document = _obj.DocumentGroup.OfficialDocuments.FirstOrDefault();
      return Functions.ApprovalReviewAssignment.CreateApprovalListStateView(document);
    }
    
    /// <summary>
    /// Получить все данные для валидации подписания одним запросом.
    /// </summary>
    /// <returns>Структура с данными валидации.</returns>
    [Remote(IsPure = true)]
    public Structures.ApprovalTask.BeforeSign ValidateBeforeSign()
    {
      var task = ApprovalTasks.As(_obj.Task);
      var document = _obj.DocumentGroup.OfficialDocuments.First();

      var errors = Functions.OfficialDocument.GetApprovalValidationErrors(document, false);
      var addenda = task.AddendaGroup.OfficialDocuments.Where(a => a.HasVersions).ToList();
      foreach (var addendum in addenda)
        errors.AddRange(Functions.OfficialDocument.GetDocumentLockErrors(addendum));

      var canSignByEmployee = Functions.OfficialDocument.CanSignByEmployee(document, Company.Employees.Current);
      var canApprove = document.AccessRights.CanApprove() && canSignByEmployee;
      var bodyChanged = Functions.ApprovalTask.DocumentHasBodyUpdateAfterLastView(document);
      return Structures.ApprovalTask.BeforeSign.Create(errors, canApprove, bodyChanged);
    }
  }
}