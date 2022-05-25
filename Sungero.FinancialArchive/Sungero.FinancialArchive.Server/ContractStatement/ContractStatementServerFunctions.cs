using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.FinancialArchive.ContractStatement;

namespace Sungero.FinancialArchive.Server
{
  partial class ContractStatementFunctions
  {
    
    public override bool CanSendAnswer()
    {
      return _obj.IsFormalized == true ?
        Exchange.PublicFunctions.ExchangeDocumentInfo.Remote.GetIncomingExDocumentInfo(_obj) != null :
        base.CanSendAnswer();
    }
    
    public override void SendAnswer(Sungero.ExchangeCore.IBusinessUnitBox box, Sungero.Parties.ICounterparty party, ICertificate certificate, bool isAgent)
    {
      if (_obj.IsFormalized == true)
      {
        Exchange.PublicFunctions.Module.SendBuyerTitle(_obj, box, certificate, isAgent);
      }
      else
      {
        base.SendAnswer(box, party, certificate, isAgent);
      }
    }
    
    /// <summary>
    /// Получить дубли актов к договорам.
    /// </summary>
    /// <param name="contractStatement">Акт.</param>
    /// <param name="businessUnit">НОР.</param>
    /// <param name="registrationNumber">Рег. номер.</param>
    /// <param name="registrationDate">Дата регистрации.</param>
    /// <param name="counterparty">Контрагент.</param>
    /// <param name="leadingDocument">Ведущий документ.</param>
    /// <returns>Дубли.</returns>
    [Remote(IsPure = true)]
    public static IQueryable<IContractStatement> GetDuplicates(IContractStatement contractStatement,
                                                               Sungero.Company.IBusinessUnit businessUnit,
                                                               string registrationNumber,
                                                               DateTime? registrationDate,
                                                               Sungero.Parties.ICounterparty counterparty,
                                                               Docflow.IOfficialDocument leadingDocument)
    {
      return ContractStatements.GetAll()
        .Where(l => Equals(contractStatement.DocumentKind, l.DocumentKind))
        .Where(l => Equals(businessUnit, l.BusinessUnit))
        .Where(l => registrationDate == l.RegistrationDate)
        .Where(l => registrationNumber == l.RegistrationNumber)
        .Where(l => Equals(counterparty, l.Counterparty))
        .Where(l => Equals(leadingDocument, l.LeadingDocument))
        .Where(l => !Equals(contractStatement, l));
    }
  }
}