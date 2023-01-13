using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Content;
using Sungero.Contracts.IncomingInvoice;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow;
using Sungero.Domain.Shared;

namespace Sungero.Contracts.Shared
{
  partial class IncomingInvoiceFunctions
  {
    #region Интеллектуальная обработка

    public override void SetRequiredProperties()
    {
      base.SetRequiredProperties();
      
      // Изменить обязательность полей в зависимости от того, программная или визуальная работа.
      var isVisualMode = ((Domain.Shared.IExtendedEntity)_obj).Params.ContainsKey(Sungero.Docflow.PublicConstants.OfficialDocument.IsVisualModeParamName);

      // При программной работе номер, дату, сумму и валюту делаем необязательными.
      _obj.State.Properties.Number.IsRequired = isVisualMode;
      _obj.State.Properties.Date.IsRequired = isVisualMode;
      _obj.State.Properties.TotalAmount.IsRequired = isVisualMode;
      _obj.State.Properties.Currency.IsRequired = isVisualMode;
      
      // При программной работе - явно сбрасываем обязательность.
      // При визуальной работе - обязательность содержания определится в вызове текущего метода в базовой сущности.
      if (!isVisualMode)
        _obj.State.Properties.Subject.IsRequired = false;
    }
    
    [Public]
    public override bool HasEmptyRequiredProperties()
    {
      return string.IsNullOrEmpty(_obj.Number) || _obj.Date == null || _obj.TotalAmount == null || _obj.Currency == null ||
        base.HasEmptyRequiredProperties();
    }
    
    [Public]
    public override bool IsVerificationModeSupported()
    {
      return true;
    }
    
    #endregion
    
    /// <summary>
    /// Изменение состояния документа для ненумеруемых документов.
    /// </summary>
    public override void SetLifeCycleState()
    {
      // Счет должен создаваться со статусом "Новый", если только это не смена типа устаревшего документа.
      if (_obj.LifeCycleState != Docflow.OfficialDocument.LifeCycleState.Obsolete)
        _obj.LifeCycleState = Docflow.OfficialDocument.LifeCycleState.Draft;
    }
    
    public override void ChangeRegistrationPaneVisibility(bool needShow, bool repeatRegister)
    {
      base.ChangeRegistrationPaneVisibility(needShow, repeatRegister);
      var notNumerable = _obj.DocumentKind != null && _obj.DocumentKind.NumberingType == Docflow.DocumentKind.NumberingType.NotNumerable;
      var canRegister = _obj.AccessRights.CanRegister();
      var caseIsEnabled = notNumerable || !notNumerable && canRegister;
      // Может быть уже закрыто от редактирования, если документ зарегистрирован и в формате номера журнала
      // присутствует индекс файла.
      caseIsEnabled = caseIsEnabled && _obj.State.Properties.CaseFile.IsEnabled;
      
      _obj.State.Properties.InternalApprovalState.IsVisible = needShow;
      _obj.State.Properties.ExecutionState.IsVisible = false;
      _obj.State.Properties.ControlExecutionState.IsVisible = false;
      _obj.State.Properties.CaseFile.IsEnabled = caseIsEnabled;
      _obj.State.Properties.PlacedToCaseFileDate.IsEnabled = caseIsEnabled;
      _obj.State.Properties.Tracking.IsEnabled = true;
    }
    
    /// <summary>
    /// Заполнить имя.
    /// </summary>
    public override void FillName()
    {
      // Не автоформируемое имя.
      if (_obj != null && _obj.DocumentKind != null && !_obj.DocumentKind.GenerateDocumentName.Value)
      {
        if (_obj.Name == Docflow.OfficialDocuments.Resources.DocumentNameAutotext)
          _obj.Name = string.Empty;
        
        if (_obj.VerificationState != null && string.IsNullOrWhiteSpace(_obj.Name))
          _obj.Name = _obj.DocumentKind.ShortName;
      }
      
      if (_obj.DocumentKind == null || !_obj.DocumentKind.GenerateDocumentName.Value)
        return;
      
      // Автоформируемое имя.
      var name = string.Empty;
      
      /* Имя в формате:
        <Вид документа> №<номер> от <дата> от <контрагент> "<содержание>".
        
        В общем случае _obj.Number != _obj.RegistrationNumber.
        В общем случае _obj.Date != _obj.RegistrationDate.
       */
      using (TenantInfo.Culture.SwitchTo())
      {
        if (!string.IsNullOrWhiteSpace(_obj.Number))
          name += Docflow.OfficialDocuments.Resources.Number + _obj.Number;
        
        if (_obj.Date != null)
          name += Docflow.OfficialDocuments.Resources.DateFrom + _obj.Date.Value.ToString("d");

        if (_obj.Counterparty != null)
          name += IncomingInvoices.Resources.NamePartForCounterparty + _obj.Counterparty.DisplayValue;
        
        if (!string.IsNullOrWhiteSpace(_obj.Subject))
          name += " \"" + _obj.Subject + "\"";
      }
      
      if (string.IsNullOrWhiteSpace(name))
      {
        name = _obj.VerificationState == null ? OfficialDocuments.Resources.DocumentNameAutotext : _obj.DocumentKind.ShortName;
      }
      else if (_obj.DocumentKind != null)
      {
        name = _obj.DocumentKind.ShortName + name;
      }
      
      name = Docflow.PublicFunctions.Module.TrimSpecialSymbols(name);
      
      _obj.Name = Docflow.PublicFunctions.OfficialDocument.AddClosingQuote(name, _obj);
    }

    /// <summary>
    /// Добавить связанные с входящим счетом документы в группу вложений.
    /// </summary>
    /// <param name="group">Группа вложений.</param>
    public override void AddRelatedDocumentsToAttachmentGroup(Sungero.Workflow.Interfaces.IWorkflowEntityAttachmentGroup group)
    {
      // Получить бухгалтерские документы.
      var accountingDocuments = _obj.Relations.GetRelatedFrom(Constants.Module.AccountingDocumentsRelationName);
      
      var documentsToAdd = accountingDocuments.Where(d => !group.All.Contains(d)).ToList();
      foreach (var document in documentsToAdd)
        group.All.Add(document);
    }
    
    /// <summary>
    /// Удалить связанные с входящим счетом документы из группы вложений.
    /// </summary>
    /// <param name="group">Группа вложений.</param>
    public override void RemoveRelatedDocumentsFromAttachmentGroup(Sungero.Workflow.Interfaces.IWorkflowEntityAttachmentGroup group)
    {
      // Получить бухгалтерские документы.
      var accountingDocuments = _obj.Relations.GetRelatedFrom(Constants.Module.AccountingDocumentsRelationName);
      
      // Удалить документы.
      var documentsToRemove = accountingDocuments.Where(d => group.All.Contains(d)).ToList();
      foreach (var document in documentsToRemove)
        group.All.Remove(document);
    }
    
    public override void UpdateLifeCycle(Enumeration? registrationState,
                                         Enumeration? approvalState,
                                         Enumeration? counterpartyApprovalState)
    {
      // Не проверять статусы для пустых параметров.
      if (_obj == null || _obj.DocumentKind == null)
        return;
      
      var lifeCycleMustBeActive = _obj.LifeCycleState == Docflow.OfficialDocument.LifeCycleState.Draft &&
        approvalState == Docflow.OfficialDocument.InternalApprovalState.Signed;
      
      if (lifeCycleMustBeActive)
        _obj.LifeCycleState = Docflow.OfficialDocument.LifeCycleState.Active;
    }
    
    /// <summary>
    /// Проверить дубли входящего счета.
    /// </summary>
    /// <param name="incomingInvoice">Счет для проверки.</param>
    /// <param name="documentKind">Вид счета.</param>
    /// <param name="number">Номер счета.</param>
    /// <param name="date">Дата счета.</param>
    /// <param name="totalAmount">Сумма счета.</param>
    /// <param name="currency">Валюта счета.</param>
    /// <param name="counterparty">Контрагент счета.</param>
    /// <returns>True, если дубликаты имеются, иначе - false.</returns>
    public static bool HaveDuplicates(IIncomingInvoice incomingInvoice,
                                      Sungero.Docflow.IDocumentKind documentKind,
                                      string number,
                                      DateTime? date,
                                      double? totalAmount,
                                      Commons.ICurrency currency,
                                      Parties.ICounterparty counterparty)
    {
      if (documentKind == null ||
          string.IsNullOrWhiteSpace(number) ||
          date == null ||
          totalAmount == null ||
          currency == null ||
          counterparty == null)
        return false;
      
      return Functions.IncomingInvoice.Remote.GetDuplicates(incomingInvoice, documentKind, number, (DateTime)date, (double)totalAmount, currency, counterparty).Any();
    }
    
    /// <summary>
    /// Заполнить свойство "Ведущий документ" в зависимости от типа документа.
    /// </summary>
    /// <param name="leadingDocument">Ведущий документ.</param>
    /// <remarks>Используется при смене типа.</remarks>
    [Public]
    public override void FillLeadingDocument(Docflow.IOfficialDocument leadingDocument)
    {
      // У Входящего счета на оплату ведущий документ хранится в свойстве Договор.
      var contractualDocument = ContractualDocuments.As(leadingDocument);
      if (contractualDocument != null && (_obj.Counterparty == null || Equals(_obj.Counterparty, contractualDocument.Counterparty)))
        _obj.Contract = contractualDocument;
      else
        _obj.Contract = null;
    }
  }
}