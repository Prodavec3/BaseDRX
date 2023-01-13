using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ConditionBase;

namespace Sungero.Docflow.Shared
{
  partial class ConditionBaseFunctions
  {
    #region Проверка привязки условий к типам документов
    
    /// <summary>
    /// Получить словарь поддерживаемых типов условий.
    /// </summary>
    /// <returns>
    /// Словарь.
    /// Ключ - GUID типа документа.
    /// Значение - список поддерживаемых условий.
    /// </returns>
    [Public]
    public virtual System.Collections.Generic.Dictionary<string, List<Enumeration?>> GetSupportedConditions()
    {
      // Все типы системы поддерживают:
      // документ по проекту
      // вид документа
      // условия по ролям согласования
      // способ доставки документа
      var types = Functions.DocumentKind.GetDocumentGuids(typeof(IOfficialDocument));
      return types.ToDictionary(t => t,
                                t => new List<Enumeration?>
                                {
                                  ConditionType.ProjectDocument,
                                  ConditionType.DocumentKind,
                                  ConditionType.RolesComparer,
                                  ConditionType.RoleEmpComparer,
                                  ConditionType.DeliveryMethod,
                                  ConditionType.SignedByCParty,
                                  ConditionType.HasAddenda,
                                  ConditionType.EmployeeInRole
                                });
    }
    
    /// <summary>
    /// Проверить возможность использования данного типа условия.
    /// </summary>
    /// <param name="documentKind">Вид документа.</param>
    /// <param name="supportedConditions">Словарь поддерживаемых условий.</param>
    /// <returns>Возможность использования данного условия.</returns>
    [Public]
    public virtual bool CheckConditionAbility(IDocumentKind documentKind, System.Collections.Generic.Dictionary<string, List<Enumeration?>> supportedConditions)
    {
      if (supportedConditions.ContainsKey(documentKind.DocumentType.DocumentTypeGuid))
        return supportedConditions[documentKind.DocumentType.DocumentTypeGuid].Contains(_obj.ConditionType);

      return false;
    }
    
    /// <summary>
    /// Получить список поддерживаемых условий.
    /// </summary>
    /// <param name="supportedConditions">Словарь поддерживаемых условий.</param>
    /// <returns>Типы условий.</returns>
    [Public]
    public virtual List<Enumeration?> GetPossibleConditionTypes(System.Collections.Generic.Dictionary<string, List<Enumeration?>> supportedConditions)
    {
      var possibleConditionTypes = new List<Enumeration?>();
      
      foreach (var documentKind in _obj.DocumentKinds.Select(x => x.DocumentKind))
      {
        if (supportedConditions.ContainsKey(documentKind.DocumentType.DocumentTypeGuid))
          possibleConditionTypes.AddRange(supportedConditions[documentKind.DocumentType.DocumentTypeGuid]);
      }
      
      return possibleConditionTypes.Distinct().ToList();
    }
    
    #endregion
    
    /// <summary>
    /// Сменить доступность реквизитов.
    /// </summary>
    public virtual void ChangePropertiesAccess()
    {
      var isAmount = _obj.ConditionType == ConditionType.AmountIsMore;
      var isCurrency = _obj.ConditionType == ConditionType.Currency;
      var isDocumentKind = _obj.ConditionType == ConditionType.DocumentKind;
      var isRolesComparer = _obj.ConditionType == ConditionType.RolesComparer;
      var isRoleEmpComparer = _obj.ConditionType == ConditionType.RoleEmpComparer;
      var isMailDelivery = _obj.ConditionType == ConditionType.DeliveryMethod;
      var isHasAddenda = _obj.ConditionType == ConditionType.HasAddenda;
      var isEmployeeInRole = _obj.ConditionType == ConditionType.EmployeeInRole;
      
      _obj.State.Properties.Amount.IsVisible = isAmount;
      _obj.State.Properties.Amount.IsRequired = isAmount;
      _obj.State.Properties.AmountOperator.IsVisible = isAmount;
      _obj.State.Properties.AmountOperator.IsRequired = isAmount;
      
      _obj.State.Properties.Currencies.IsVisible = isCurrency;
      _obj.State.Properties.Currencies.IsRequired = isCurrency;

      _obj.State.Properties.ConditionDocumentKinds.IsVisible = isDocumentKind;
      _obj.State.Properties.ConditionDocumentKinds.IsRequired = isDocumentKind;
      
      _obj.State.Properties.ApprovalRole.IsVisible = isRolesComparer || isRoleEmpComparer || isEmployeeInRole;
      _obj.State.Properties.ApprovalRole.IsRequired = isRolesComparer || isRoleEmpComparer || isEmployeeInRole;
      _obj.State.Properties.ApprovalRoleForComparison.IsVisible = isRolesComparer;
      _obj.State.Properties.ApprovalRoleForComparison.IsRequired = isRolesComparer;
      _obj.State.Properties.RecipientForComparison.IsVisible = isRoleEmpComparer || isEmployeeInRole;
      _obj.State.Properties.RecipientForComparison.IsRequired = isRoleEmpComparer || isEmployeeInRole;
      
      _obj.State.Properties.DeliveryMethods.IsVisible = isMailDelivery;
      _obj.State.Properties.DeliveryMethods.IsRequired = isMailDelivery;
      
      _obj.State.Properties.AddendaDocumentKind.IsVisible = isHasAddenda;
      _obj.State.Properties.AddendaDocumentKind.IsRequired = isHasAddenda;
    }
    
    /// <summary>
    /// Очистка скрытых свойств.
    /// </summary>
    public virtual void ClearHiddenProperties()
    {
      if (!_obj.State.Properties.Amount.IsVisible)
        _obj.Amount = null;
      
      if (!_obj.State.Properties.AmountOperator.IsVisible)
        _obj.AmountOperator = null;
      
      if (!_obj.State.Properties.Currencies.IsVisible)
        _obj.Currencies.Clear();
      
      if (!_obj.State.Properties.ConditionDocumentKinds.IsVisible)
        _obj.ConditionDocumentKinds.Clear();
      
      if (!_obj.State.Properties.ApprovalRole.IsVisible)
        _obj.ApprovalRole = null;
      
      if (!_obj.State.Properties.ApprovalRoleForComparison.IsVisible)
        _obj.ApprovalRoleForComparison = null;
      
      if (!_obj.State.Properties.RecipientForComparison.IsVisible)
        _obj.RecipientForComparison = null;
      
      if (!_obj.State.Properties.DeliveryMethods.IsVisible)
        _obj.DeliveryMethods.Clear();

      if (!_obj.State.Properties.AddendaDocumentKind.IsVisible)
        _obj.AddendaDocumentKind = null;
    }
    
    /// <summary>
    /// Проверить условие.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="task">Задача на согласование.</param>
    /// <returns>Результат проверки условия. Структуру формата - выполнение условия, сообщение об ошибке.</returns>
    public virtual Structures.ConditionBase.ConditionResult CheckCondition(IOfficialDocument document, IApprovalTask task)
    {
      if (_obj.ConditionType == ConditionType.AmountIsMore)
        return this.CheckAmountIsMore(document, task);
      
      if (_obj.ConditionType == ConditionType.Nonresident)
        return this.CheckNonresident(document, task);
      
      if (_obj.ConditionType == ConditionType.Currency)
        return this.CheckCurrency(document, task);
      
      if (_obj.ConditionType == ConditionType.ProjectDocument)
        return this.CheckProjectDocument(document);
      
      if (_obj.ConditionType == ConditionType.DocumentKind)
        return this.CheckDocumentKind(document);
      
      if (_obj.ConditionType == ConditionType.RolesComparer)
        return Functions.ConditionBase.Remote.CompareRoles(_obj, task);
      
      if (_obj.ConditionType == ConditionType.RoleEmpComparer)
        return Functions.ConditionBase.Remote.CompareRoleAndRecipient(_obj, task);
      
      if (_obj.ConditionType == ConditionType.DeliveryMethod)
        return this.CheckDeliveryMethod(document, task);
      
      if (_obj.ConditionType == ConditionType.SignedByCParty)
        return this.CheckSignedByCounterparty(document, task);
      
      if (_obj.ConditionType == ConditionType.HasAddenda)
        return this.CheckHasAddenda(document, task);
      
      if (_obj.ConditionType == ConditionType.EmployeeInRole)
        return Functions.ConditionBase.Remote.CheckEmployeeInRole(_obj, task);

      return Structures.ConditionBase.ConditionResult.Create(null, ConditionBases.Resources.CannotPerformConditionCheck);
    }
    
    /// <summary>
    /// Проверить сумму документа.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="task">Задача на согласование.</param>
    /// <returns>Результат проверки условия. Структуру формата - выполнение условия (true, если сумма документа больше или равна сумме условия), сообщение об ошибке.</returns>
    private Structures.ConditionBase.ConditionResult CheckAmountIsMore(IOfficialDocument document, IApprovalTask task)
    {
      if (!AccountingDocumentBases.Is(document) && !ContractualDocumentBases.Is(document))
        return Structures.ConditionBase.ConditionResult.Create(null, ConditionBases.Resources.DocumentNotContainsFieldAmount);
      
      var accounting = AccountingDocumentBases.As(document);
      var contractual = ContractualDocumentBases.As(document);

      if (accounting != null && accounting.TotalAmount.HasValue)
        return Structures.ConditionBase.ConditionResult.Create(this.CheckAmount(accounting.TotalAmount), string.Empty);

      if (contractual != null && contractual.TotalAmount.HasValue)
        return Structures.ConditionBase.ConditionResult.Create(this.CheckAmount(contractual.TotalAmount), string.Empty);
      
      return Structures.ConditionBase.ConditionResult.Create(null, ConditionBases.Resources.FillTotalAmountInContractCard);
    }
    
    /// <summary>
    /// Проверить соответствие суммы документа условию.
    /// </summary>
    /// <param name="documentAmount">Сумма документа.</param>
    /// <returns>True, если сумма соответствует условию. False, если не соответствует. Null, если условие не определено.</returns>
    private bool? CheckAmount(double? documentAmount)
    {
      if (_obj.AmountOperator == AmountOperator.GreaterThan)
        return documentAmount > _obj.Amount;
      if (_obj.AmountOperator == AmountOperator.GreaterOrEqual)
        return documentAmount >= _obj.Amount;
      if (_obj.AmountOperator == AmountOperator.LessThan)
        return documentAmount < _obj.Amount;
      if (_obj.AmountOperator == AmountOperator.LessOrEqual)
        return documentAmount <= _obj.Amount;
      
      return null;
    }
    
    /// <summary>
    /// Проверить наличие проекта в документе.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>Результат проверки условия. Структуру формата - выполнение условия (true, если проект указан в документе), сообщение об ошибке.</returns>
    private Structures.ConditionBase.ConditionResult CheckProjectDocument(IOfficialDocument document)
    {
      if (document != null)
        return Structures.ConditionBase.ConditionResult.Create(Functions.OfficialDocument.IsProjectDocument(document, new List<int>()), string.Empty);
      return Structures.ConditionBase.ConditionResult.Create(null, string.Empty);
    }
    
    /// <summary>
    /// Проверить контрагента на резидент/нерезидент.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="task">Задача на согласование.</param>
    /// <returns>Результат проверки условия. Структуру формата - выполнение условия (true, если контрагент нерезидент), сообщение об ошибке.</returns>
    private Structures.ConditionBase.ConditionResult CheckNonresident(IOfficialDocument document, IApprovalTask task)
    {
      if (OutgoingDocumentBases.Is(document))
      {
        var outgoingDocument = OutgoingDocumentBases.As(document);
        // Если хоть один адресат нерезидент, идем по ветке с нерезидентом.
        var isNonResident = outgoingDocument.Addressees.Any(x => x.Correspondent.Nonresident == true);
        return Structures.ConditionBase.ConditionResult.Create(isNonResident, string.Empty);
      }
      
      if (ContractualDocumentBases.Is(document))
      {
        var counterparty = ContractualDocumentBases.As(document).Counterparty;
        var nonresident = counterparty != null ? counterparty.Nonresident : true;
        return Structures.ConditionBase.ConditionResult.Create(nonresident, string.Empty);
      }
      
      if (AccountingDocumentBases.Is(document))
      {
        var counterparty = AccountingDocumentBases.As(document).Counterparty;
        var nonresident = counterparty != null ? counterparty.Nonresident : true;
        return Structures.ConditionBase.ConditionResult.Create(nonresident, string.Empty);
      }
      
      return Structures.ConditionBase.ConditionResult.Create(null, ConditionBases.Resources.DocumentNotContainsFieldCounterpartyOrCorrespondent);
    }
    
    /// <summary>
    /// Проверить валюту.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="task">Задача на согласование.</param>
    /// <returns>Результат проверки условия. Структуру формата - выполнение условия (true, если валюта совпадает), сообщение об ошибке.</returns>
    private Structures.ConditionBase.ConditionResult CheckCurrency(IOfficialDocument document, IApprovalTask task)
    {
      var accountingDocument = AccountingDocumentBases.As(document);
      var contractualDocument = ContractualDocumentBases.As(document);

      if (accountingDocument == null && contractualDocument == null)
        return Structures.ConditionBase.ConditionResult.Create(null, ConditionBases.Resources.DocumentNotContainsFieldCurrency);
      
      if (accountingDocument != null && accountingDocument.Currency != null)
        return Structures.ConditionBase.ConditionResult.Create(_obj.Currencies.Any(c => Equals(c.Currency, accountingDocument.Currency)), string.Empty);
      
      if (contractualDocument != null && contractualDocument.Currency != null)
        return Structures.ConditionBase.ConditionResult.Create(_obj.Currencies.Any(c => Equals(c.Currency, contractualDocument.Currency)), string.Empty);
      
      return Structures.ConditionBase.ConditionResult.Create(null, ConditionBases.Resources.FillCurrencyInContractCard);
    }

    /// <summary>
    /// Проверить вид документа.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>Результат проверки условия. Структуру формата - выполнение условия (true, если вид документа в списке), сообщение об ошибке.</returns>
    private Structures.ConditionBase.ConditionResult CheckDocumentKind(IOfficialDocument document)
    {
      if (document != null)
        return Structures.ConditionBase.ConditionResult.Create(_obj.ConditionDocumentKinds.Any(d => Equals(d.DocumentKind, document.DocumentKind)), string.Empty);
      
      return Structures.ConditionBase.ConditionResult.Create(null, string.Empty);
    }
    
    /// <summary>
    /// Проверить способ доставки документа.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="task">Задача на согласование.</param>
    /// <returns>Результат проверки условия. Структуру формата - выполнение условия (true, если способ доставки документа в списке).</returns>
    private Structures.ConditionBase.ConditionResult CheckDeliveryMethod(IOfficialDocument document, IApprovalTask task)
    {
      var deliveryMethod = task != null ? task.DeliveryMethod : document.DeliveryMethod;
      return Structures.ConditionBase.ConditionResult
        .Create(_obj.DeliveryMethods.Any(d => Equals(d.DeliveryMethod, deliveryMethod)), string.Empty);
    }
    
    /// <summary>
    /// Проверить подписанность документа контрагентом.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="task">Задача на согласование.</param>
    /// <returns>Результат проверки условия. Структуру формата - выполнение условия (true, если документ подписан контрагентом).</returns>
    public virtual Structures.ConditionBase.ConditionResult CheckSignedByCounterparty(IOfficialDocument document, IApprovalTask task)
    {
      if (task != null && task.DocumentExternalApprovalState != null)
        return Structures.ConditionBase.ConditionResult.Create(task.DocumentExternalApprovalState == Docflow.ApprovalTask.DocumentExternalApprovalState.Signed, string.Empty);
      if (document != null)
        return Structures.ConditionBase.ConditionResult.Create(document.ExternalApprovalState == Docflow.OfficialDocument.ExternalApprovalState.Signed, string.Empty);
      
      return Structures.ConditionBase.ConditionResult.Create(null, string.Empty);
    }
    
    /// <summary>
    /// Проверить наличие вложения в группу приложений с нужным видом.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="task">Задача на согласование.</param>
    /// <returns>Результат проверки условия. Структуру формата - выполнение условия (true, если вложен документ с нужным видом).</returns>
    public virtual Structures.ConditionBase.ConditionResult CheckHasAddenda(IOfficialDocument document, IApprovalTask task)
    {
      if (task != null)
        return Structures.ConditionBase.ConditionResult.Create(task.AddendaGroup.OfficialDocuments.Any(a => Equals(a.DocumentKind, _obj.AddendaDocumentKind)), string.Empty);
      
      return Structures.ConditionBase.ConditionResult.Create(null, string.Empty);
    }
  }
}