using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow;

namespace Sungero.RecordManagementUI.Server
{
  public class ModuleFunctions
  {
    #region Поиски

    /// <summary>
    /// Получить все регистрируемые документы.
    /// </summary>
    /// <param name="registrationNumber">Регистрационный номер.</param>
    /// <param name="registrationDateFrom">Зарегистрирован от.</param>
    /// <param name="registrationDateTo">Зарегистрирован по.</param>
    /// <param name="documentRegister">Журнал регистрации.</param>
    /// <param name="caseFile">Дело.</param>
    /// <param name="registeredBy">Кем зарегистрирован.</param>
    /// <returns>Список документов, удовлетворяющий условиям.</returns>
    [Remote(IsPure = true)]
    public static IQueryable<IOfficialDocument> GetFilteredRegisteredDocuments(
      string registrationNumber, DateTime? registrationDateFrom, DateTime? registrationDateTo, IDocumentRegister documentRegister, ICaseFile caseFile, Company.IEmployee registeredBy)
    {
      var documents = OfficialDocuments.GetAll();

      if (registrationNumber != null)
        documents = documents.Where(l => l.RegistrationNumber.Contains(registrationNumber));

      if (registrationDateFrom != null)
        documents = documents.Where(l => l.RegistrationDate >= registrationDateFrom);

      if (registrationDateTo != null)
      {
        registrationDateTo = registrationDateTo.Value.Date.EndOfDay();
        documents = documents.Where(l => l.RegistrationDate <= registrationDateTo);
      }

      if (documentRegister != null)
        documents = documents.Where(l => Equals(l.DocumentRegister, documentRegister));

      if (caseFile != null)
        documents = documents.Where(l => Equals(l.CaseFile, caseFile));

      if (registeredBy != null)
      {
        // TODO Zamerov: ересь с операциями.
        var regitrationOperation = new Enumeration(Docflow.PublicFunctions.OfficialDocument.GetRegistrationOperation());
        documents = documents.WhereDocumentHistory(h => Equals(h.User, registeredBy) && h.Operation == regitrationOperation);
      }

      return documents;
    }

    /// <summary>
    /// Получить документы с указанным корреспондентом.
    /// </summary>
    /// <param name="counterparty">Контрагент-корреспондент.</param>
    /// <param name="periodBegin">Документы от.</param>
    /// <param name="periodEnd">Документы по.</param>
    /// <returns>Связанные с корреспондентом документы.</returns>
    [Remote(IsPure = true)]
    public static IQueryable<IOfficialDocument> GetOfficialCorrespondenceWithCounterparty(
      Parties.ICounterparty counterparty, DateTime? periodBegin, DateTime? periodEnd)
    {
      return OfficialDocuments.GetAll()
        .Where(l => l.RegistrationState == Docflow.OfficialDocument.RegistrationState.Reserved ||
               l.RegistrationState == Docflow.OfficialDocument.RegistrationState.Registered)
        .Where(l => periodBegin == null || l.RegistrationDate >= periodBegin)
        .Where(l => periodEnd == null || l.RegistrationDate <= periodEnd)
        .Where(l => (IncomingDocumentBases.Is(l) && Equals(IncomingDocumentBases.As(l).Correspondent, counterparty)) ||
               (OutgoingDocumentBases.Is(l) && OutgoingDocumentBases.As(l).Addressees.Select(x => x.Correspondent).Any(y => Equals(y, counterparty))));
    }

    #endregion
  }
}