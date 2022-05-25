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
    public override List<ISignatureSetting> GetSignatureSettings()
    {
      var baseSettings = base.GetSignatureSettings();
      if (_obj.LeadingDocument != null)
        baseSettings.AddRange(Functions.OfficialDocument.GetSignatureSettings(_obj.LeadingDocument));
      return baseSettings;
    }
  }
}