using System;
using System.Collections.Generic;
using System.Linq;
using CommonLibrary;
using Sungero.Contracts.ContractBase;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Contracts.Server
{
  partial class ContractBaseFunctions
  {
    /// <summary>
    /// Получить дубли договора.
    /// </summary>
    /// <param name="contract">Договор.</param>
    /// <param name="businessUnit">НОР.</param>
    /// <param name="registrationNumber">Рег. номер.</param>
    /// <param name="registrationDate">Дата регистрации.</param>
    /// <param name="counterparty">Контрагент.</param>
    /// <returns>Дубли.</returns>
    [Remote(IsPure = true)]
    public static IQueryable<IContractBase> GetDuplicates(IContractBase contract,
                                                          Sungero.Company.IBusinessUnit businessUnit,
                                                          string registrationNumber,
                                                          DateTime? registrationDate,
                                                          Sungero.Parties.ICounterparty counterparty)
    {
      return ContractBases.GetAll()
        .Where(l => Equals(contract.DocumentKind, l.DocumentKind))
        .Where(l => Equals(businessUnit, l.BusinessUnit))
        .Where(l => registrationDate == l.RegistrationDate)
        .Where(l => registrationNumber == l.RegistrationNumber)
        .Where(l => Equals(counterparty, l.Counterparty))
        .Where(l => !Equals(contract, l));
    }

    /// <summary>
    /// Получить часть имени договора игнорируя права доступа.
    /// </summary>
    /// <param name="contractId">Договор.</param>
    /// <returns>Часть имени.</returns>
    [Remote(IsPure = true)]
    public static string GetNamePartByContractIgnoreAccessRights(int contractId)
    {
      return Functions.ContractBase.GetNamePartByContract(Functions.ContractualDocument.GetIgnoreAccessRights(contractId));
    }
    
    /// <summary>
    /// Получить правила согласования для договоров.
    /// </summary>
    /// <returns>Правила согласования, удовлетворяющие договору.</returns>
    [Remote]
    public override List<Sungero.Docflow.IApprovalRuleBase> GetApprovalRules()
    {
      return base.GetApprovalRules()
        .Select(r => ContractsApprovalRules.As(r))
        .Where(r => r != null)
        .OrderByDescending(r => r.Priority)
        .ToList<Sungero.Docflow.IApprovalRuleBase>();
    }
    
    /// <summary>
    /// Построить сводку по документу.
    /// </summary>
    /// <returns>Сводка по документу.</returns>
    [Remote(IsPure = true)]
    public override StateView GetDocumentSummary()
    {
      var documentSummary = StateView.Create();
      var documentBlock = documentSummary.AddBlock();
      
      // Краткое имя документа.
      var documentName = _obj.DocumentKind.Name;
      if (!string.IsNullOrWhiteSpace(_obj.RegistrationNumber))
        documentName += Docflow.OfficialDocuments.Resources.Number + _obj.RegistrationNumber;
      
      if (_obj.RegistrationDate != null)
        documentName += Docflow.OfficialDocuments.Resources.DateFrom + _obj.RegistrationDate.Value.ToString("d");
      
      documentBlock.AddLabel(documentName);
      
      // Типовой/Не типовой.
      var isStandardLabel = _obj.IsStandard.Value ? ContractBases.Resources.isStandartContract : ContractBases.Resources.isNotStandartContract;
      documentBlock.AddLabel(string.Format("({0})", isStandardLabel));
      documentBlock.AddLineBreak();
      documentBlock.AddLineBreak();
      
      // НОР.
      documentBlock.AddLabel(string.Format("{0}: ", _obj.Info.Properties.BusinessUnit.LocalizedName));
      if (_obj.BusinessUnit != null)
        documentBlock.AddLabel(Hyperlinks.Get(_obj.BusinessUnit));
      else
        documentBlock.AddLabel("-");
      
      documentBlock.AddLineBreak();
      
      // Контрагент.
      documentBlock.AddLabel(string.Format("{0}:", ContractBases.Resources.Counterparty));
      if (_obj.Counterparty != null)
      {
        documentBlock.AddLabel(Hyperlinks.Get(_obj.Counterparty));
        if (_obj.Counterparty.Nonresident == true)
          documentBlock.AddLabel(string.Format("({0})", _obj.Counterparty.Info.Properties.Nonresident.LocalizedName).ToLower());
      }
      else
      {
        documentBlock.AddLabel("-");
      }
      
      documentBlock.AddLineBreak();
      
      // Содержание.
      var subject = !string.IsNullOrEmpty(_obj.Subject) ? _obj.Subject : "-";
      documentBlock.AddLabel(string.Format("{0}: {1}", ContractBases.Resources.Subject, subject));
      documentBlock.AddLineBreak();
      
      // Сумма договора.
      var amount = this.GetTotalAmountDocumentSummary(_obj.TotalAmount, _obj.Currency);
      var amountText = string.Format("{0}: {1}", _obj.Info.Properties.TotalAmount.LocalizedName, amount);
      documentBlock.AddLabel(amountText);
      documentBlock.AddLineBreak();
      
      // Срок действия договора.
      var validity = "-";
      var validFrom = _obj.ValidFrom.HasValue ?
        string.Format("{0} {1} ", ContractBases.Resources.From, _obj.ValidFrom.Value.Date.ToShortDateString()) :
        string.Empty;
      
      var validTill = _obj.ValidTill.HasValue ?
        string.Format("{0} {1}", ContractBases.Resources.Till, _obj.ValidTill.Value.Date.ToShortDateString()) :
        string.Empty;
      
      var isAutomaticRenewal = _obj.IsAutomaticRenewal.Value &&  !string.IsNullOrEmpty(validTill) ?
        string.Format(", {0}", ContractBases.Resources.Renewal) :
        string.Empty;
      
      if (!string.IsNullOrEmpty(validFrom) || !string.IsNullOrEmpty(validTill))
        validity = string.Format("{0}{1}{2}", validFrom, validTill, isAutomaticRenewal);
      
      var validityText = string.Format("{0}:", ContractBases.Resources.Validity);
      documentBlock.AddLabel(validityText);
      documentBlock.AddLabel(validity);
      documentBlock.AddLineBreak();
      documentBlock.AddEmptyLine();
      
      // Примечание.
      var note = string.IsNullOrEmpty(_obj.Note) ? "-" : _obj.Note;
      var noteText = string.Format("{0}:", ContractBases.Resources.Note);
      documentBlock.AddLabel(noteText);
      documentBlock.AddLabel(note);
      
      return documentSummary;
    }
    
    /// <summary>
    /// Изменить статус документа на "В разработке".
    /// </summary>
    public override void SetLifeCycleStateDraft()
    {
      base.SetLifeCycleStateDraft();
      
      if (_obj.LifeCycleState == Sungero.Contracts.ContractBase.LifeCycleState.Terminated)
      {
        Logger.DebugFormat("UpdateLifeCycleState: Document {0} changed LifeCycleState to 'Draft'.", _obj.Id);
        _obj.LifeCycleState = Docflow.OfficialDocument.LifeCycleState.Draft;
      }
    }
    
    /// <summary>
    /// Проверить, связан ли документ специализированной связью.
    /// </summary>
    /// <returns>True - если связан, иначе - false.</returns>
    [Remote(IsPure = true)]
    public override bool HasSpecifiedTypeRelations()
    {
      var hasSpecifiedTypeRelations = false;
      AccessRights.AllowRead(
        () =>
        {
          hasSpecifiedTypeRelations = SupAgreements.GetAll().Any(x => Equals(x.LeadingDocument, _obj));
        });
      return base.HasSpecifiedTypeRelations() || hasSpecifiedTypeRelations;
    }
  }
}