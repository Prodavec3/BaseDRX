using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.OutgoingLetter;

namespace Sungero.RecordManagement.Server
{
  partial class OutgoingLetterFunctions
  {
    /// <summary>
    /// Создать сопроводительное письмо.
    /// </summary>
    /// <param name="document">Договорной документ, к которому создается письмо.</param>
    /// <returns>Письмо.</returns>
    [Remote, Public]
    public static IOutgoingLetter CreateCoverLetter(Sungero.Docflow.IOfficialDocument document)
    {
      var letter = OutgoingLetters.Create();
      letter.Subject = string.Format("{0}{1}", OutgoingLetters.Resources.Sending, document.Name);
      letter.BusinessUnit = document.BusinessUnit;
      letter.DeliveryMethod = document.DeliveryMethod;
      Docflow.PublicFunctions.OfficialDocument.CopyProjects(document, letter);
      
      var contractualDocument = Sungero.Docflow.ContractualDocumentBases.As(document);
      if (contractualDocument != null)
        letter.Correspondent = contractualDocument.Counterparty;
      var financialDocument = Sungero.Docflow.AccountingDocumentBases.As(document);
      if (financialDocument != null)
        letter.Correspondent = financialDocument.Counterparty;
      
      return letter;
    }
  }
}