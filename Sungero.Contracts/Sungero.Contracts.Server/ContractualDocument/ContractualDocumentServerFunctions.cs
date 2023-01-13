using System;
using System.Collections.Generic;
using System.Linq;
using CommonLibrary;
using Sungero.Company;
using Sungero.Contracts.ContractualDocument;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow;
using Sungero.Parties;
using DeclensionCase = Sungero.Core.DeclensionCase;

namespace Sungero.Contracts.Server
{
  partial class ContractualDocumentFunctions
  {
    /// <summary>
    /// Получить договорной документ игнорируя права доступа.
    /// </summary>
    /// <param name="documentId">ИД документа.</param>
    /// <returns>Договорной документ.</returns>
    public static IContractualDocument GetIgnoreAccessRights(int documentId)
    {
      // HACK Котегов: использование внутренней сессии для обхода прав доступа.
      Logger.DebugFormat("GetIgnoreAccessRights: contractId {0}", documentId);
      using (var session = new Sungero.Domain.Session())
      {
        var innerSession = (Sungero.Domain.ISession)session.GetType()
          .GetField("InnerSession", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(session);
        
        return ContractualDocuments.As((Sungero.Domain.Shared.IEntity)innerSession.Get(typeof(IContractualDocument), documentId));
      }
    }
    
    /// <summary>
    /// Получить номер регистрации игнорируя права доступа.
    /// </summary>
    /// <param name="documentId">Договорной документ.</param>
    /// <returns>Номер регистрации.</returns>
    [Remote(IsPure = true), Public]
    public static string GetRegistrationNumberIgnoreAccessRights(int documentId)
    {
      return Functions.ContractualDocument.GetIgnoreAccessRights(documentId).RegistrationNumber;
    }

    /// <summary>
    /// Получить для договорного документа сумму прописью с валютой.
    /// </summary>
    /// <param name="contractualDocument">Договорной документ.</param>
    /// <returns>Сумма прописью с валютой.</returns>
    [Converter("TotalAmountInCurrencyToWords")]
    public static string TotalAmountInCurrencyToWords(IContractualDocument contractualDocument)
    {
      
      if (contractualDocument.TotalAmount == null || contractualDocument.Currency == null)
        return null;
      
      var totalAmount = contractualDocument.TotalAmount.Value;
      var integerPart = (long)Math.Truncate(totalAmount);
      var fractionalPart = (int)Math.Round((totalAmount - integerPart) * 100);
      
      var contractCurrencyShortName = contractualDocument.Currency.ShortName.Trim();
      var currencyName = contractCurrencyShortName.EndsWith(Constants.ContractualDocument.ShortNameEnd) ?
        contractCurrencyShortName :
        StringUtils.NumberDeclension(integerPart,
                                     contractCurrencyShortName,
                                     Sungero.Core.CaseConverter.ConvertCurrencyNameToTargetDeclension(contractCurrencyShortName, DeclensionCase.Genitive),
                                     Sungero.Core.CaseConverter.ConvertCurrencyNameToTargetDeclension(contractCurrencyShortName.Pluralize(), DeclensionCase.Genitive));
      
      var contractCurrencyFractionName = contractualDocument.Currency.FractionName.Trim();
      var currencyFractionalName = contractCurrencyFractionName.EndsWith(Constants.ContractualDocument.ShortNameEnd) ?
        contractCurrencyFractionName :
        StringUtils.NumberDeclension(fractionalPart,
                                     contractCurrencyFractionName,
                                     Sungero.Core.CaseConverter.ConvertCurrencyNameToTargetDeclension(contractCurrencyFractionName, DeclensionCase.Genitive),
                                     Sungero.Core.CaseConverter.ConvertCurrencyNameToTargetDeclension(contractCurrencyFractionName.Pluralize(), DeclensionCase.Genitive));
      
      return string.Format("{0},{1} {2} ({3} {2} {1} {4})",
                           integerPart.ToString("N0"), fractionalPart.ToString("D2"), currencyName,
                           StringUtils.NumberToWords(integerPart).Capitalize(), currencyFractionalName);
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
          hasSpecifiedTypeRelations = IncomingInvoices.GetAll().Any(x => Equals(x.Contract, _obj));
        });
      return base.HasSpecifiedTypeRelations() || hasSpecifiedTypeRelations;
    }
  }
}