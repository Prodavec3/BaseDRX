using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.FormalizedPowerOfAttorney;
using XmlElementNames = Sungero.Docflow.Constants.FormalizedPowerOfAttorney.XmlElementNames;
using XmlFPoAInfoAttributeNames = Sungero.Docflow.Constants.FormalizedPowerOfAttorney.XmlFPoAInfoAttributeNames;
using XmlIssuedToAttributeNames = Sungero.Docflow.Constants.FormalizedPowerOfAttorney.XmlIssuedToAttributeNames;

namespace Sungero.Docflow.Server
{
  partial class FormalizedPowerOfAttorneyFunctions
  {
    #region Импорт МЧД из xml
    
    /// <summary>
    /// Загрузить тело эл. доверенности из XML и импортировать внешнюю подпись.
    /// </summary>
    /// <param name="xml">Структура с XML.</param>
    /// <param name="signature">Структура с подписью.</param>
    [Remote, Public]
    public virtual void ImportFormalizedPowerOfAttorneyFromXmlAndSign(Docflow.Structures.Module.IByteArray xml,
                                                                      Docflow.Structures.Module.IByteArray signature)
    {
      this.ValidateFormalizedPowerOfAttorneyXml(xml);
      
      signature = this.ConvertSignatureFromBase64(signature);
      this.VerifyExternalSignature(xml, signature);
      
      this.FillFormalizedPowerOfAttorney(xml);
      
      this.WriteFormalizedPowerOfAttorneyBody(xml);
      
      Functions.FormalizedPowerOfAttorney.SetActiveLifeCycleState(_obj);
      
      // Сохранение необходимо для импорта подписи.
      _obj.Save();
      
      // Сохранение записи об импорте xml-файла в историю.
      var importFromXmlOperationText = Constants.FormalizedPowerOfAttorney.Operation.ImportFromXml;
      var importFromXmlComment = Sungero.Docflow.FormalizedPowerOfAttorneys.Resources.ImportFromXmlHistoryComment;
      _obj.History.Write(new Enumeration(importFromXmlOperationText), null, importFromXmlComment, _obj.LastVersion.Number);
      
      this.ImportSignature(xml, signature);
      this.CheckSignature();
    }
    
    /// <summary>
    /// Декодировать подпись из base64.
    /// </summary>
    /// <param name="signature">Подпись.</param>
    /// <returns>Декодированная подпись.</returns>
    [Public]
    public virtual Docflow.Structures.Module.IByteArray ConvertSignatureFromBase64(Docflow.Structures.Module.IByteArray signature)
    {
      var signatureInfo = ExternalSignatures.GetSignatureInfo(signature.Bytes);
      // Если подпись передали в закодированном виде, попытаться раскодировать.
      if (signatureInfo.SignatureFormat == SignatureFormat.Hash)
      {
        try
        {
          var byteString = System.Text.Encoding.UTF8.GetString(signature.Bytes);
          var signatureBytes = Convert.FromBase64String(byteString);
          signature = Docflow.Structures.Module.ByteArray.Create(signatureBytes);
        }
        catch
        {
          Logger.Error("Import formalized power of attorney. Failed to import signature: cannot decode given signature.");
          throw AppliedCodeException.Create(FormalizedPowerOfAttorneys.Resources.SignatureImportFailed);
        }
      }
      
      return signature;
    }
    
    /// <summary>
    /// Проверить валидность xml-файла эл. доверенности.
    /// </summary>
    /// <param name="xml">Тело эл. доверенности.</param>
    [Public]
    public virtual void ValidateFormalizedPowerOfAttorneyXml(Docflow.Structures.Module.IByteArray xml)
    {
      var memoryStream = new System.IO.MemoryStream(xml.Bytes);
      System.Xml.Linq.XDocument xdoc;
      try
      {
        xdoc = System.Xml.Linq.XDocument.Load(memoryStream);
      }
      catch (Exception e)
      {
        Logger.ErrorFormat("Import formalized power of attorney. Failed to load XML: {0}", e.Message);
        throw AppliedCodeException.Create(FormalizedPowerOfAttorneys.Resources.XmlLoadFailed);
      }
      finally
      {
        xdoc = null;
        memoryStream.Close();
      }
    }
    
    /// <summary>
    /// Проверить подпись на достоверность.
    /// </summary>
    /// <param name="xml">Подписанные данные.</param>
    /// <param name="signature">Подпись.</param>
    [Public]
    public virtual void VerifyExternalSignature(Docflow.Structures.Module.IByteArray xml, Docflow.Structures.Module.IByteArray signature)
    {
      using (var xmlStream = new System.IO.MemoryStream(xml.Bytes))
      {
        var signatureInfo = ExternalSignatures.Verify(signature.Bytes, xmlStream);
        if (signatureInfo.Errors.Any())
        {
          Logger.ErrorFormat("Import formalized power of attorney. Failed to import signature: {0}", string.Join("\n", signatureInfo.Errors.Select(x => x.Message)));
          throw AppliedCodeException.Create(FormalizedPowerOfAttorneys.Resources.SignatureImportFailed);
        }
      }
    }
    
    /// <summary>
    /// Заполнить свойства эл. доверенности.
    /// </summary>
    /// <param name="xml">Тело эл. доверенности.</param>
    [Public]
    public virtual void FillFormalizedPowerOfAttorney(Docflow.Structures.Module.IByteArray xml)
    {
      System.Xml.Linq.XDocument xdoc;
      using (var memoryStream = new System.IO.MemoryStream(xml.Bytes))
        xdoc = System.Xml.Linq.XDocument.Load(memoryStream);
      
      var poaInfo = this.TryGetPoaInfoElement(xdoc, XmlElementNames.PowerOfAttorney,
                                              XmlElementNames.Document,
                                              XmlElementNames.PowerOfAttorneyInfo);
      
      this.FillUnifiedRegistrationNumberFromXml(xdoc,
                                                poaInfo,
                                                XmlFPoAInfoAttributeNames.UnifiedRegistrationNumber);
      
      this.FillValidDatesFromXml(xdoc,
                                 poaInfo,
                                 XmlFPoAInfoAttributeNames.ValidFrom,
                                 XmlFPoAInfoAttributeNames.ValidTill);
      
      // Получить регистрационные данные из xml и попытаться пронумеровать документ.
      // Если в xml нет даты регистрации, но есть номер, взять текущую дату в качестве даты регистрации.
      string number = this.GetAttributeValueByName(poaInfo, XmlFPoAInfoAttributeNames.RegistrationNumber);
      DateTime? date = this.GetDateFromXml(poaInfo, XmlFPoAInfoAttributeNames.RegistrationDate) ?? Calendar.Today;
      this.FillRegistrationData(number, date);
      this.FillIssuedToFromXml(xdoc);
      
      this.FillDocumentName(xdoc);
    }
    
    /// <summary>
    /// Получить XML-элемент с информацией об эл. доверенности.
    /// </summary>
    /// <param name="xdoc">XML-документ.</param>
    /// <param name="poaElementName">Имя элемента, содержащего доверенность.</param>
    /// <param name="documentElementName">Имя элемента, содержащего документ.</param>
    /// <param name="poaInfoElementName">Имя элемента, содержащего информацию о доверенности.</param>
    /// <returns>XML-элемент с информацией о доверенности.</returns>
    [Public]
    public virtual System.Xml.Linq.XElement TryGetPoaInfoElement(System.Xml.Linq.XDocument xdoc,
                                                                 string poaElementName,
                                                                 string documentElementName,
                                                                 string poaInfoElementName)
    {
      try
      {
        return xdoc.Element(poaElementName).Element(documentElementName).Element(poaInfoElementName);
      }
      catch (Exception ex)
      {
        Logger.ErrorFormat("Import formalized power of attorney. Failed to parse given XML as formalized power of attorney: {0}",
                           ex.Message);
        throw AppliedCodeException.Create(FormalizedPowerOfAttorneys.Resources.XmlLoadFailed);
      }
    }
    
    /// <summary>
    /// Заполнить единый рег. номер эл. доверенности из xml-файла.
    /// </summary>
    /// <param name="xdoc">Тело доверенности в xml-формате.</param>
    /// <param name="powerOfAttorneyInfo">Xml-элемент с информацией об эл. доверенности.</param>
    /// <param name="poaUnifiedRegNumberAttributeName">Имя атрибута, содержащего единый рег.номер доверенности.</param>
    [Public]
    public virtual void FillUnifiedRegistrationNumberFromXml(System.Xml.Linq.XDocument xdoc,
                                                             System.Xml.Linq.XElement powerOfAttorneyInfo,
                                                             string poaUnifiedRegNumberAttributeName)
    {
      var unifiedRegNumber = this.GetAttributeValueByName(powerOfAttorneyInfo, poaUnifiedRegNumberAttributeName);
      
      Guid guid;
      if (!Guid.TryParse(unifiedRegNumber, out guid))
      {
        throw AppliedCodeException.Create(FormalizedPowerOfAttorneys.Resources.XmlLoadFailed,
                                          FormalizedPowerOfAttorneys.Resources.ErrorValidateUnifiedRegistrationNumber);
      }
      
      _obj.UnifiedRegistrationNumber = guid.ToString();
    }
    
    /// <summary>
    /// Заполнить дату начала и окончания действия эл. доверенности из xml-файла.
    /// </summary>
    /// <param name="xdoc">Тело доверенности в xml-формате.</param>
    /// <param name="powerOfAttorneyInfo">Xml-элемент с информацией об эл. доверенности.</param>
    /// <param name="poaValidFromAttributeName">Имя атрибута, содержащего дату начала действия доверенности.</param>
    /// <param name="poaValidTillAttributeName">Имя атрибута, содержащего дату окончания действия доверенности.</param>
    [Public]
    public virtual void FillValidDatesFromXml(System.Xml.Linq.XDocument xdoc,
                                              System.Xml.Linq.XElement powerOfAttorneyInfo,
                                              string poaValidFromAttributeName,
                                              string poaValidTillAttributeName)
    {
      DateTime? validFrom;
      DateTime? validTill;
      try
      {
        validFrom = this.GetDateFromXml(powerOfAttorneyInfo, poaValidFromAttributeName);
        validTill = this.GetDateFromXml(powerOfAttorneyInfo, poaValidTillAttributeName);
      }
      catch (Exception ex)
      {
        Logger.Error("Import formalized power of attorney. Failed to parse validity dates from xml.", ex);
        throw AppliedCodeException.Create(FormalizedPowerOfAttorneys.Resources.XmlLoadFailed);
      }
      if (validFrom == null || validTill == null)
      {
        Logger.Error("Import formalized power of attorney. Failed to parse validity dates from xml.");
        throw AppliedCodeException.Create(FormalizedPowerOfAttorneys.Resources.XmlLoadFailed);
      }
      _obj.ValidFrom = validFrom;
      _obj.ValidTill = validTill;
    }
    
    /// <summary>
    /// Заполнить рег. данные эл. доверенности в зависимости от настроек вида документа.
    /// </summary>
    /// <param name="number">Регистрационный номер.</param>
    /// <param name="date">Дата регистрации.</param>
    /// <remarks>Если вид документа ненумеруемый, данные не будут заполнены.</remarks>
    [Public]
    public virtual void FillRegistrationData(string number, DateTime? date)
    {
      if (string.IsNullOrEmpty(number) || !date.HasValue)
        return;
      
      // Проверить настройки RX на возможность нумерации документа.
      if (_obj.DocumentKind == null || _obj.DocumentKind.NumberingType == Docflow.DocumentKind.NumberingType.NotNumerable)
        return;
      
      if (_obj.DocumentKind.NumberingType == Docflow.DocumentKind.NumberingType.Numerable)
      {
        var matchingRegistersIds = Functions.OfficialDocument.GetDocumentRegistersIdsByDocument(_obj, Docflow.RegistrationSetting.SettingType.Numeration);
        if (matchingRegistersIds.Count == 1)
        {
          var register = DocumentRegisters.Get(matchingRegistersIds.First());
          Functions.OfficialDocument.RegisterDocument(_obj, register, date, number, false, false);
          return;
        }
      }
      if (_obj.AccessRights.CanRegister())
      {
        _obj.RegistrationDate = date;
        _obj.RegistrationNumber = number;
      }
      else
      {
        var registrationDataString = FormalizedPowerOfAttorneys.Resources.FormalizedPowerOfAttorneyFormat(_obj.DocumentKind.ShortName,
                                                                                                          number,
                                                                                                          date.Value.Date.ToString("d"));
        _obj.Note = registrationDataString + Environment.NewLine + _obj.Note;
      }
      return;
    }
    
    /// <summary>
    /// Заполнить поле Кому эл. доверенности из xml-файла.
    /// </summary>
    /// <param name="xdoc">Тело доверенности в xml-формате.</param>
    [Public]
    public virtual void FillIssuedToFromXml(System.Xml.Linq.XDocument xdoc)
    {
      // Не перезаполнять Кому.
      if (_obj.IssuedTo != null)
        return;
      
      // Получить ИНН, СНИЛС и ФИО из xml.
      var issuedToInfoFromXml = this.GetIssuedToInfoFromXml(xdoc);
      
      // Попытаться заполнить Кому по ИНН.
      var employees = Company.PublicFunctions.Employee.Remote.GetEmployeesByTIN(issuedToInfoFromXml.TIN);
      if (employees.Count() == 1)
      {
        _obj.IssuedTo = employees.FirstOrDefault();
        return;
      }
      
      // Попытаться заполнить Кому по СНИЛС.
      employees = Company.PublicFunctions.Employee.Remote.GetEmployeesByINILA(issuedToInfoFromXml.INILA);
      if (employees.Count() == 1)
      {
        _obj.IssuedTo = employees.FirstOrDefault();
        return;
      }
      
      // Попытаться заполнить Кому по ФИО.
      if (_obj.IssuedTo == null)
        _obj.IssuedTo = Company.PublicFunctions.Employee.Remote.GetEmployeeByName(issuedToInfoFromXml.FullName);
    }
    
    /// <summary>
    /// Заполнить имя эл. доверенности.
    /// </summary>
    /// <param name="xdoc">Тело доверенности в xml-формате.</param>
    [Public]
    public virtual void FillDocumentName(System.Xml.Linq.XDocument xdoc)
    {
      // Заполнить пустое имя документа из сокращенного имени вида документа.
      if (string.IsNullOrWhiteSpace(_obj.Name) && _obj.DocumentKind != null && _obj.DocumentKind.GenerateDocumentName != true)
        _obj.Name = _obj.DocumentKind.ShortName;
    }
    
    /// <summary>
    /// Записать тело доверенности в версию.
    /// </summary>
    /// <param name="xml">Структура с телом доверенности.</param>
    [Public]
    public virtual void WriteFormalizedPowerOfAttorneyBody(Docflow.Structures.Module.IByteArray xml)
    {
      var memoryStream = new System.IO.MemoryStream(xml.Bytes);
      _obj.LastVersion.Body.Write(memoryStream);
    }
    
    /// <summary>
    /// Импортировать подпись.
    /// </summary>
    /// <param name="xml">Структура с подписанными данными.</param>
    /// <param name="signature">Структура с подписью.</param>
    /// <remarks>В случае если подпись без даты, которая в Sungero обязательна, будет выполнена попытка проставить подпись
    /// хоть как-нибудь. Подпись после этого будет отображаться как невалидная, но она хотя бы будет.
    /// Валидная подпись останется только в сервисе.</remarks>
    [Public]
    public virtual void ImportSignature(Docflow.Structures.Module.IByteArray xml, Docflow.Structures.Module.IByteArray signature)
    {
      var signatureBytes = signature.Bytes;

      // Получить подписавшего из сертификата.
      var certificateInfo = Docflow.PublicFunctions.Module.GetSignatureCertificateInfo(signatureBytes);
      var signatoryName = Docflow.PublicFunctions.Module.GetCertificateSignatoryName(certificateInfo.SubjectInfo);
      
      // Импортировать подпись.
      Signatures.Import(_obj, SignatureType.Approval, signatoryName, signatureBytes, _obj.LastVersion);
    }
    
    /// <summary>
    /// Проверить, что документ подписан. Если нет, сгенерировать исключение.
    /// </summary>
    [Public]
    public virtual void CheckSignature()
    {
      Sungero.Domain.Shared.ISignature importedSignature;
      importedSignature = Signatures.Get(_obj.LastVersion)
        .Where(s => s.IsExternal == true && s.SignCertificate != null)
        .OrderByDescending(x => x.Id)
        .FirstOrDefault();
      
      if (importedSignature == null)
      {
        Logger.DebugFormat("Can't find signature on document with version id: '{0}'", _obj.LastVersion.Id);
        throw AppliedCodeException.Create(FormalizedPowerOfAttorneys.Resources.SignatureImportFailed);
      }
    }
    
    /// <summary>
    /// Получить дату из информации об эл. доверенности из xml-файла.
    /// </summary>
    /// <param name="element">Элемент с датой.</param>
    /// <param name="attributeName">Наименование атрибута для даты.</param>
    /// <returns>Дата.</returns>
    [Public]
    public virtual DateTime? GetDateFromXml(System.Xml.Linq.XElement element, string attributeName)
    {
      var dateValue = this.GetAttributeValueByName(element, attributeName);
      if (string.IsNullOrEmpty(dateValue))
        return null;
      
      DateTime date;
      if (Calendar.TryParseDate(dateValue, out date))
        return date;
      
      return Convert.ToDateTime(dateValue);
    }
    
    /// <summary>
    /// Получить из xml информацию об уполномоченном представителе.
    /// </summary>
    /// <param name="xdoc">Тело доверенности в xml-формате.</param>
    /// <returns>Структура с информацией.</returns>
    [Public]
    public virtual Structures.FormalizedPowerOfAttorney.IIssuedToInfo GetIssuedToInfoFromXml(System.Xml.Linq.XDocument xdoc)
    {
      var result = Structures.FormalizedPowerOfAttorney.IssuedToInfo.Create();
      
      // Получить элементы, связанные с уполномоченным представителем.
      var representativeElements = xdoc
        ?.Element(XmlElementNames.PowerOfAttorney)
        ?.Element(XmlElementNames.Document)
        ?.Element(XmlElementNames.AuthorizedRepresentative)
        ?.Elements(XmlElementNames.Representative);
      
      // Не искать по сотрудникам, если в xml нет узлов или больше одного узла с уполномоченным представителем, который является физ. лицом.
      if (representativeElements == null || !representativeElements.Any() || representativeElements.Count() > 1)
        return result;
      
      var representativeElement = representativeElements.FirstOrDefault();
      if (representativeElement == null)
        return result;
      
      // Получить ИНН, СНИЛС и ФИО уполномоченного представителя.
      var individualElement = representativeElement.Element(XmlElementNames.Individual);
      var tin = this.GetAttributeValueByName(individualElement, XmlIssuedToAttributeNames.TIN);
      var inila = this.GetAttributeValueByName(individualElement, XmlIssuedToAttributeNames.INILA);
      var fullName = this.GetIssuedToFullNameFromXml(individualElement);
      
      return Structures.FormalizedPowerOfAttorney.IssuedToInfo.Create(fullName, tin, inila);
    }
    
    /// <summary>
    /// Получить имя того, кому выдана эл. доверенность из xml-файла.
    /// </summary>
    /// <param name="individualElement">Элемент xml с информацией о полномочном представителе.</param>
    /// <returns>ФИО.</returns>
    [Public]
    public virtual string GetIssuedToFullNameFromXml(System.Xml.Linq.XElement individualElement)
    {
      var individualNameElement = individualElement.Element(XmlIssuedToAttributeNames.IndividualName);
      if (individualNameElement == null)
        return string.Empty;
      
      // Собрать полные ФИО из фамилии, имени и отчества.
      var parts = new List<string>();
      var surname = this.GetAttributeValueByName(individualNameElement, XmlIssuedToAttributeNames.LastName);
      if (!string.IsNullOrWhiteSpace(surname))
        parts.Add(surname);
      var name = this.GetAttributeValueByName(individualNameElement, XmlIssuedToAttributeNames.FirstName);
      if (!string.IsNullOrWhiteSpace(name))
        parts.Add(name);
      var patronymic = this.GetAttributeValueByName(individualNameElement, XmlIssuedToAttributeNames.MiddleName);
      if (!string.IsNullOrWhiteSpace(patronymic))
        parts.Add(patronymic);
      
      var fullName = string.Join(" ", parts);
      return fullName;
    }
    
    /// <summary>
    /// Получить значение атрибута по имени.
    /// </summary>
    /// <param name="element">Элемент, которому принадлежит атрибут.</param>
    /// <param name="attributeName">Имя атрибута.</param>
    /// <returns>Значение или пустая строка, если атрибут не найден.</returns>
    [Public]
    public virtual string GetAttributeValueByName(System.Xml.Linq.XElement element, string attributeName)
    {
      var attribute = element.Attribute(attributeName);
      return attribute == null ? string.Empty : attribute.Value;
    }
    
    #endregion

    #region Поиск дублей
    
    /// <summary>
    /// Получить дубли эл. доверенности.
    /// </summary>
    /// <returns>Дубли эл. доверенности.</returns>
    [Public, Remote(IsPure = true)]
    public virtual List<IFormalizedPowerOfAttorney> GetFormalizedPowerOfAttorneyDuplicates()
    {
      var duplicates = new List<IFormalizedPowerOfAttorney>();
      if (_obj.IssuedTo == null ||
          _obj.BusinessUnit == null ||
          string.IsNullOrEmpty(_obj.UnifiedRegistrationNumber))
      {
        return duplicates;
      }
      
      AccessRights.AllowRead(
        () =>
        {
          duplicates = FormalizedPowerOfAttorneys
            .GetAll()
            .Where(f => !Equals(f, _obj) && f.LifeCycleState == LifeCycleState.Active &&
                   f.UnifiedRegistrationNumber == _obj.UnifiedRegistrationNumber &&
                   Equals(f.IssuedTo, _obj.IssuedTo) &&
                   Equals(f.BusinessUnit, _obj.BusinessUnit))
            .ToList();
        });
      return duplicates;
    }
    
    #endregion
    
    /// <summary>
    /// Проверить, отключена ли валидация рег.номера.
    /// </summary>
    /// <returns>Для МЧД всегда отключена.</returns>
    public override bool IsNumberValidationDisabled()
    {
      return true;
    }
  }
}