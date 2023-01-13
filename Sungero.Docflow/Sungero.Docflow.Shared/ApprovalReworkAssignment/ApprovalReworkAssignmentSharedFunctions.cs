using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ApprovalReworkAssignment;
using Sungero.Domain.Shared;

namespace Sungero.Docflow.Shared
{
  partial class ApprovalReworkAssignmentFunctions
  {
    /// <summary>
    /// Обновить отображение доставки.
    /// </summary>
    public virtual void UpdateDeliveryMethod()
    {
      var document = _obj.DocumentGroup.OfficialDocuments.FirstOrDefault();
      
      // Не давать изменять способ доставки для исходящих писем на несколько адресатов
      if (OutgoingDocumentBases.Is(document) && OutgoingDocumentBases.As(document).IsManyAddressees == true)
      {
        _obj.State.Properties.DeliveryMethod.IsEnabled = false;
        _obj.State.Properties.ExchangeService.IsEnabled = false;
      }
      else
      {
        var deliveryMethodIsExchange = _obj.DeliveryMethod != null && _obj.DeliveryMethod.Sid == Constants.MailDeliveryMethod.Exchange;
        _obj.State.Properties.ExchangeService.IsEnabled = deliveryMethodIsExchange;
        _obj.State.Properties.ExchangeService.IsRequired = deliveryMethodIsExchange;
        
        if (deliveryMethodIsExchange && document != null)
        {
          var formParams = ((IExtendedEntity)_obj).Params;
          bool isIncomingDocument = false;
          if (formParams.ContainsKey(Constants.ApprovalReworkAssignment.IsIncomingDocument))
            isIncomingDocument = (bool)formParams[Constants.ApprovalReworkAssignment.IsIncomingDocument];
          else
          {
            isIncomingDocument = Docflow.PublicFunctions.OfficialDocument.Remote.CanSendAnswer(document);
            formParams[Constants.ApprovalReworkAssignment.IsIncomingDocument] = isIncomingDocument;
          }
          var isFormalizedDocument = Docflow.AccountingDocumentBases.Is(document) && Docflow.AccountingDocumentBases.As(document).IsFormalized == true;
          _obj.State.Properties.DeliveryMethod.IsEnabled = !isIncomingDocument;
          _obj.State.Properties.ExchangeService.IsEnabled = !(isIncomingDocument || isFormalizedDocument);
        }
      }
    }
    
    /// <summary>
    /// Обновить доступность полей карточки задачи.
    /// </summary>
    public virtual void UpdatePropertiesEnableState()
    {
      var document = _obj.DocumentGroup.OfficialDocuments.FirstOrDefault();
      if (_obj.ForwardPerformer == null)
      {
        if (_obj.Status.Value == Workflow.AssignmentBase.Status.InProcess)
        {
          _obj.State.Properties.Signatory.IsEnabled = true;
          _obj.State.Properties.Addressee.IsEnabled = true;
          _obj.State.Properties.Addressees.IsEnabled = true;
          _obj.State.Properties.DeliveryMethod.IsEnabled = true;
          _obj.State.Properties.ExchangeService.IsEnabled = true;
          _obj.State.Properties.Approvers.IsEnabled = true;
        }
        
        // Не давать менять адресата в согласовании служебных записок.
        if (Memos.Is(document))
        {
          _obj.State.Properties.Addressee.IsEnabled = false;
          _obj.State.Properties.Addressees.IsEnabled = false;
        }
        
        Functions.ApprovalReworkAssignment.UpdateDeliveryMethod(_obj);
      }
      else
      {
        _obj.State.Properties.Signatory.IsEnabled = false;
        _obj.State.Properties.Addressee.IsEnabled = false;
        _obj.State.Properties.Addressees.IsEnabled = false;
        _obj.State.Properties.DeliveryMethod.IsEnabled = false;
        _obj.State.Properties.ExchangeService.IsEnabled = false;
        _obj.State.Properties.Approvers.IsEnabled = false;
      }
    }
    
    /// <summary>
    /// Валидация задания на доработку.
    /// </summary>
    /// <param name="approvalStages">Этапы регламента.</param>
    /// <param name="e">Аргументы действия.</param>
    /// <returns>True, если валидация прошла успешно, и False, если были ошибки.</returns>
    public virtual bool ValidateApprovalReworkAssignment(Structures.Module.DefinedApprovalStages approvalStages, Sungero.Core.IValidationArgs e)
    {
      var hasError = false;
      var deletedApprovers = _obj.State.Properties.Approvers.Deleted;
      var addedApprovers = _obj.State.Properties.Approvers.Added;
      
      // Запрещено изменять действие, если результат согласования отрицателен.
      if (_obj.Approvers.Any(a => a.Approved == Sungero.Docflow.ApprovalReworkAssignmentApprovers.Approved.NotApproved &&
                             a.Action != Sungero.Docflow.ApprovalReworkAssignmentApprovers.Action.SendForApproval))
      {
        e.AddError(ApprovalReworkAssignments.Resources.CannotChangeAction);
        hasError = true;
      }

      // Запрещено удалять обязательных согласующих.
      if (deletedApprovers.Any(app => app.IsRequiredApprover == true))
      {
        e.AddError(ApprovalReworkAssignments.Resources.CannotDeleteRequiredApprovers);
        hasError = true;
      }

      // Запрещено добавлять согласующих, если нет этапа согласования с дополнительными согласующими.
      if (addedApprovers.Any(a => a.IsRequiredApprover != true) &&
          !approvalStages.Stages.Any(s => s.StageType == Docflow.ApprovalStage.StageType.Approvers && s.Stage.AllowAdditionalApprovers == true))
      {
        e.AddError(ApprovalReworkAssignments.Resources.CannotAddApproversIfNoAdditionalInRule);
        hasError = true;
      }
      
      return !hasError;
    }
    
    /// <summary>
    /// Синхронизировать адресатов из переданных значений.
    /// </summary>
    /// <param name="addressees">Список адресатов.</param>
    /// <param name="addressee">Адресат.</param>
    public virtual void SynchronizeAddresses(List<Company.IEmployee> addressees, IEmployee addressee)
    {
      this.SetAddressees(addressees);
      this.FillAddresseeFromAddressees();
      _obj.Addressee = addressee;
    }
    
    /// <summary>
    /// Очистить адресатов и заполнить первого адресата из карточки.
    /// </summary>
    public virtual void ClearAddresseesAndFillFirstAddressee()
    {
      this.SetAddressees(new List<IEmployee>() { _obj.Addressee });
    }
    
    /// <summary>
    /// Заполнить адресата из коллекции адресатов.
    /// </summary>
    public virtual void FillAddresseeFromAddressees()
    {
      var addressee = _obj.Addressees.OrderBy(a => a.Id).FirstOrDefault(a => a.Addressee != null);
      if (addressee != null)
        _obj.Addressee = addressee.Addressee;
      else
        _obj.Addressee = null;
    }
    
    /// <summary>
    /// Задать адресатов задания на доработку.
    /// </summary>
    /// <param name="addressees">Адресаты.</param>
    public virtual void SetAddressees(List<IEmployee> addressees)
    {
      _obj.Addressees.Clear();
      if (addressees == null)
        return;
      addressees = addressees.Where(x => x != null).Distinct().ToList();
      foreach (var addressee in addressees)
        _obj.Addressees.AddNew().Addressee = addressee;
    }
  }
}