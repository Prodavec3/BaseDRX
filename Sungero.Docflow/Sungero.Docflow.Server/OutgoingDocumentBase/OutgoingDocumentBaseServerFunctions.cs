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
    /// Получить текст обращения к адресату письма.
    /// </summary>
    /// <param name="document">Исходящий документ.</param>
    /// <returns>Текст обращения.</returns>
    [Sungero.Core.Converter("AddresseeAppeal")]
    public static string AddresseeAppeal(IOutgoingDocumentBase document)
    {
      if (document.IsManyAddressees.Value)
        return null;

      Sungero.Core.Enumeration? sex;
      var addressee = document.Addressee;
      if (Sungero.Parties.People.Is(document.Correspondent))
        sex = Sungero.Parties.People.As(document.Correspondent).Sex.Value;
      else if (addressee != null && addressee.Person != null)
        sex = addressee.Person.Sex.Value;
      else if (addressee != null && addressee.Person == null)
      {
        CommonLibrary.PersonFullName personFullName;
        if (CommonLibrary.PersonFullName.TryParse(addressee.Name, out personFullName))
        {
          var middleName = personFullName.MiddleName;
          var gender = CaseConverter.DefineGender(middleName);
          if (gender == Gender.NotDefined)
            return null;
          
          sex = gender == Gender.Masculine ? Parties.Person.Sex.Male : Parties.Person.Sex.Female;
        }
        else
          return null;
      }
      else
        return null;
      
      return sex == Parties.Person.Sex.Male
        ? Sungero.Docflow.OutgoingDocumentBases.Resources.AddresseeAppealRespectedMale
        : Sungero.Docflow.OutgoingDocumentBases.Resources.AddresseeAppealRespectedFemale;
    }

    /// <summary>
    /// Получить имя и отчество адресата письма.
    /// </summary>
    /// <param name="document">Исходящий документ.</param>
    /// <returns>Имя и отчество.</returns>
    [Sungero.Core.Converter("AddresseeNameAndPatronymic")]
    public static string AddresseeNameAndPatronymic(IOutgoingDocumentBase document)
    {
      if (document.IsManyAddressees.Value)
        return null;
      
      if (Sungero.Parties.People.Is(document.Correspondent))
      {
        var correspondent = Sungero.Parties.People.As(document.Correspondent);
        return string.Format("{0} {1}", correspondent.FirstName, correspondent.MiddleName);
      }
      
      var addressee = document.Addressee;
      if (addressee == null)
        return null;
      
      if (addressee.Person != null)
        return string.Format("{0} {1}", addressee.Person.FirstName, addressee.Person.MiddleName);
      
      CommonLibrary.PersonFullName personFullName;
      if (CommonLibrary.PersonFullName.TryParse(addressee.Name, out personFullName))
        return string.Format("{0} {1}", personFullName.FirstName, personFullName.MiddleName);
      else
        return null;
      
    }
    
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
    [Public, Sungero.Core.Converter("GetAddressees")]
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
          result += person == null ? addressee.Correspondent.Name : Parties.PublicFunctions.Person.GetLastNameAndInitials(person, Core.DeclensionCase.Dative);
          result += Environment.NewLine;
          
          // Не выводить ФИО адресата для персоны.
          if (person == null)
          {
            var addresseeName = string.Format("<{0}>", OutgoingDocumentBases.Resources.LastNameAndInitials);
            // И.О. Фамилия адресата в дательном падеже.
            if (addressee.Addressee != null)
            {
              addresseeName = addressee.Addressee.Name;
              if (addressee.Addressee.Person != null)
                addresseeName = Parties.PublicFunctions.Person.GetLastNameAndInitials(addressee.Addressee.Person, Core.DeclensionCase.Dative);
              else
              {
                CommonLibrary.PersonFullName personFullName;
                if (CommonLibrary.PersonFullName.TryParse(addressee.Addressee.Name, out personFullName))
                {
                  personFullName.DisplayFormat = CommonLibrary.PersonFullNameDisplayFormat.LastNameAndInitials;
                  addresseeName = CaseConverter.ConvertPersonFullNameToTargetDeclension(personFullName, Sungero.Core.DeclensionCase.Dative);
                }
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
          hasSpecifiedTypeRelations = IncomingDocumentBases.GetAll().Any(x => Equals(x.InResponseTo, _obj));
        });
      return base.HasSpecifiedTypeRelations() || hasSpecifiedTypeRelations;
    }
  }
}