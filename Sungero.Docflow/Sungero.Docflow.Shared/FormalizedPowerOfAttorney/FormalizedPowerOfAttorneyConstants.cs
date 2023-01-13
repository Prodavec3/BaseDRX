using System;
using Sungero.Core;

namespace Sungero.Docflow.Constants
{
  public static class FormalizedPowerOfAttorney
  {
    /// <summary>
    /// Названия элементов в теле эл. доверенности.
    /// </summary>
    public static class XmlElementNames
    {
      public const string PowerOfAttorney = "Доверенность";
      public const string Document = "Документ";
      public const string PowerOfAttorneyInfo = "СвДов";
      public const string AuthorizedRepresentative = "СвУпПред";
      public const string Representative = "СвПред";
      public const string Individual = "СведФизЛ";
    }
    
    /// <summary>
    /// Названия атрибутов уполномоченного лица в теле эл. доверенности.
    /// </summary>
    public static class XmlIssuedToAttributeNames
    {
      public const string TIN = "ИННФЛ";
      public const string INILA = "СНИЛС";
      
      public const string IndividualName = "ФИО";
      public const string LastName = "Фамилия";
      public const string FirstName = "Имя";
      public const string MiddleName = "Отчество";
    }
    
    /// <summary>
    /// Названия атрибутов информации о доверенности в теле эл. доверенности.
    /// </summary>
    public static class XmlFPoAInfoAttributeNames
    {
      public const string UnifiedRegistrationNumber = "НомДовер";
      public const string ValidFrom = "ДатаВыдДовер";
      public const string ValidTill = "ДатаКонДовер";
      public const string RegistrationNumber = "ВнНомДовер";
      public const string RegistrationDate = "ДатаВнРегДовер";
    }

    public static class Operation
    {
      /// <summary>
      /// Импорт эл. доверенности из xml-файла.
      /// </summary>
      public const string ImportFromXml = "ImportFromXml";
    }
    
    /// <summary>
    /// Код диалога импорта эл. доверенности из xml-файла.
    /// </summary>
    public const string ImportFromXmlHelpCode = "Sungero_ImportFormalizedPowerOfAttorneyFromXmlDialog";
    
    // Параметр "Закрыть возможность импорта эл. доверенности".
    [Public]
    public const string HideImportParamName = "HideImport";
  }
}