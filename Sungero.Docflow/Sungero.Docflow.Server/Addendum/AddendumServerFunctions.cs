using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.Addendum;

namespace Sungero.Docflow.Server
{
  partial class AddendumFunctions
  {
    /// <summary>
    /// Получить документ игнорируя права доступа.
    /// </summary>
    /// <param name="documentId">ИД документа.</param>
    /// <returns>Документ.</returns>
    public static IOfficialDocument GetOfficialDocumentIgnoreAccessRights(int documentId)
    {
      // HACK Котегов: использование внутренней сессии для обхода прав доступа.
      Logger.DebugFormat("GetOfficialDocumentIgnoreAccessRights: documentId {0}", documentId);
      using (var session = new Sungero.Domain.Session())
      {
        var innerSession = (Sungero.Domain.ISession)session.GetType()
          .GetField("InnerSession", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(session);
        
        return OfficialDocuments.As((Sungero.Domain.Shared.IEntity)innerSession.Get(typeof(IOfficialDocument), documentId));
      }
    }
    
    /// <summary>
    /// Создать приложение к документу.
    /// </summary>
    /// <returns>Приложение.</returns>
    [Remote]
    public static IAddendum Create()
    {
      return Addendums.Create();
    }
    
    /// <summary>
    /// Получить документ игнорируя права доступа.
    /// </summary>
    /// <param name="documentId">ИД документа.</param>
    /// <returns>Документ.</returns>
    [Remote(IsPure = true)]
    public static Sungero.Docflow.Structures.Addendum.LeadingDocument GetLeadingDocument(int documentId)
    {
      var document = GetOfficialDocumentIgnoreAccessRights(documentId);
      return Sungero.Docflow.Structures.Addendum.LeadingDocument.Create(document.Name, document.RegistrationNumber);
    }
    
    /// <summary>
    /// Получить права подписи приложения.
    /// </summary>
    /// <returns>Права подписи на приложение и ведущий документ.</returns>
    [Obsolete("Используйте метод GetSignatureSettingsQuery")]
    public override List<ISignatureSetting> GetSignatureSettings()
    {
      var baseSettings = base.GetSignatureSettings();
      if (_obj.LeadingDocument != null)
        baseSettings.AddRange(Functions.OfficialDocument.GetSignatureSettings(_obj.LeadingDocument));
      return baseSettings;
    }
    
    /// <summary>
    /// Получить права подписи приложения.
    /// </summary>
    /// <returns>Права подписи на приложение и ведущий документ.</returns>
    public override IQueryable<ISignatureSetting> GetSignatureSettingsQuery()
    {
      var settings = base.GetSignatureSettingsQuery();
      
      return this.GetSignatureSettingsWithLeadingDocument(settings);
    }
    
    /// <summary>
    /// Получить наши организации для фильтрации подходящих прав подписи.
    /// </summary>
    /// <returns>Наши организации.</returns>
    /// <remarks>У не нумеруемых приложений не заполняется "Наша организация", поэтому "Наша организация" берется из ведущего документа.</remarks>
    public override List<Sungero.Company.IBusinessUnit> GetBusinessUnits()
    {
      var businessUnits = base.GetBusinessUnits();
      
      if (!businessUnits.Any())
      {
        var leadingDocument = _obj.LeadingDocument;
        
        var addendaIds = new List<int>();
        addendaIds.Add(_obj.Id);

        while (leadingDocument != null && leadingDocument.BusinessUnit == null && !addendaIds.Contains(leadingDocument.Id))
        {
          addendaIds.Add(leadingDocument.Id);
          leadingDocument = leadingDocument.LeadingDocument;
        }
        
        if (leadingDocument != null && leadingDocument.BusinessUnit != null)
          businessUnits.Add(leadingDocument.BusinessUnit);
        else
          Logger.DebugFormat("Failed to identify business unit for addendum {0}.", _obj.Id);
      }
      
      return businessUnits;
    }
    
    /// <summary>
    /// Получить права подписи приложения.
    /// </summary>
    /// <param name="settings">Список прав подписи на приложения.</param>
    /// <returns>Права подписи на приложение и ведущий документ.</returns>
    public virtual IQueryable<ISignatureSetting> GetSignatureSettingsWithLeadingDocument(IQueryable<ISignatureSetting> settings)
    {
      if (_obj.LeadingDocument != null && !this.IsCircularAddenda())
      {
        var settingIds = settings.Select(s => s.Id).ToList();
        settingIds.AddRange(Functions.OfficialDocument.GetSignatureSettingsQuery(_obj.LeadingDocument)
                            .Where(s => !settingIds.Contains(s.Id))
                            .Select(s => s.Id)
                            .ToList());
        
        return SignatureSettings.GetAll(s => settingIds.Contains(s.Id));
      }
      else
        return settings;
    }
    
    /// <summary>
    /// Проверить, находится ли приложение в цепочке с зацикленными (ссылающимися друг на друга) приложениями.
    /// </summary>
    /// <returns>True - если приложения зациклены (ссылаются друг на друга).</returns>
    public bool IsCircularAddenda()
    {
      var leadingDocument = _obj.LeadingDocument;
      var addendaIds = new List<int>();
      addendaIds.Add(_obj.Id);
      
      while (leadingDocument != null)
      {        
        if (addendaIds.Contains(leadingDocument.Id))
          return true;
        
        addendaIds.Add(leadingDocument.Id);
        leadingDocument = leadingDocument.LeadingDocument;
      }
      
      return false;
    }
    
    /// <summary>
    /// Восстановить связь приложения с ведущим документом.
    /// </summary>
    /// <param name="addendumId">Идентификатор приложения.</param>
    [Remote]
    public static void RestoreAddendumRelationToLeadingDocument(int addendumId)
    {
      var addendum = Addendums.GetAll(x => x.Id == addendumId).FirstOrDefault();
      if (addendum == null || addendum.LeadingDocument == null)
        return;
      
      var hasRelationToLeadingDocument = addendum.Relations.GetRelatedFrom(Constants.Module.AddendumRelationName)
        .Any(x => Equals(x, addendum.LeadingDocument));
      if (hasRelationToLeadingDocument)
        return;
      
      addendum.Relations.AddFrom(Constants.Module.AddendumRelationName, addendum.LeadingDocument);
      addendum.Relations.Save();
    }
  }
}