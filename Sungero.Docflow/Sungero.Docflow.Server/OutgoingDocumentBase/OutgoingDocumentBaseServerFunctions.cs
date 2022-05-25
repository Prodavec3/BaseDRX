using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.OutgoingDocumentBase;

namespace Sungero.Docflow.Server
{
  partial class OutgoingDocumentBaseFunctions
  {
          
    /// <summary>
    /// Получить договор, если он был связан с исходящим документом.
    /// </summary>
    /// <param name="document">Исходящий документ.</param>
    /// <returns>Договор.</returns>
    [Sungero.Core.Converter("Contract")]
    public static IOfficialDocument Contract(IOutgoingDocumentBase document)
    {
      // Вернуть договор, если он был связан с исходящим письмом.
      var contractTypeGuid = Guid.Parse("f37c7e63-b134-4446-9b5b-f8811f6c9666");
      var contracts = document.Relations.GetRelatedFrom(Constants.Module.CorrespondenceRelationName).Where(d => d.TypeDiscriminator == contractTypeGuid);
      return Sungero.Docflow.OfficialDocuments.As(contracts.FirstOrDefault());
    }
    
    /// <summary>
    /// Получить список адресатов для шаблона исходящего письма.
    /// </summary>
    /// <param name="document">Исходящее письмо.</param>
    /// <returns>Список адресатов.</returns>
    [Sungero.Core.Converter("GetAddressees")]
    public static string GetAddressees(IOutgoingDocumentBase document)
    {
      var result = string.Empty;
      if (document.Addressees.Count() > Constants.Module.AddresseesShortListLimit)
        result = OfficialDocuments.Resources.ToManyAddressees;
      else
      {
        foreach (var addressee in document.Addressees.OrderBy(a => a.Number))
        {
          var person = Sungero.Parties.People.As(addressee.Correspondent);
          // Не выводить должность для персоны.
          if (person == null)
          {
            // Должность адресата в дательном падеже.
            var jobTitle = string.Format("<{0}>", OfficialDocuments.Resources.JobTitle);
            if (addressee.Addressee != null && !string.IsNullOrEmpty(addressee.Addressee.JobTitle))
              jobTitle = CaseConverter.ConvertJobTitleToTargetDeclension(addressee.Addressee.JobTitle, Sungero.Core.DeclensionCase.Dative);

            result += jobTitle;
            result += Environment.NewLine;
          }
          
          // Организация адресата/ФИО Персоны.
          if (person == null)
            result += addressee.Correspondent.Name;
          else
          {
            var personName = CommonLibrary.PersonFullName.Create(person.LastName, person.FirstName, person.MiddleName, CommonLibrary.PersonFullNameDisplayFormat.LastNameAndInitials);
            result += CaseConverter.ConvertPersonFullNameToTargetDeclension(personName, Sungero.Core.DeclensionCase.Dative);
          }
          
          result += Environment.NewLine;
          
          // Не выводить ФИО адресата для персоны.
          if (person == null)
          {
            var addresseeName = string.Format("<{0}>", OutgoingDocumentBases.Resources.InitialsAndLastName);
            // И.О. Фамилия адресата в дательном падеже.
            if (addressee.Addressee != null)
            {
              addresseeName = addressee.Addressee.Name;
              if (addressee.Addressee.Person != null)
              {
                var personFullName = CommonLibrary.PersonFullName.Create(addressee.Addressee.Person.LastName,
                                                                         addressee.Addressee.Person.FirstName,
                                                                         addressee.Addressee.Person.MiddleName,
                                                                         CommonLibrary.PersonFullNameDisplayFormat.InitialsAndLastName);
                addresseeName = CaseConverter.ConvertPersonFullNameToTargetDeclension(personFullName, Sungero.Core.DeclensionCase.Dative);
              }
            }
            result += addresseeName;
            result += Environment.NewLine;
          }
          
          // Адрес доставки.
          var postalAddress = string.Format("<{0}>", OutgoingDocumentBases.Resources.PostalAddress);
          if (!string.IsNullOrEmpty(addressee.Correspondent.PostalAddress))
            postalAddress = addressee.Correspondent.PostalAddress;
          result += postalAddress;
          result += Environment.NewLine;
          result += Environment.NewLine;
        }
      }
      return result.Trim();
    }
  }
}