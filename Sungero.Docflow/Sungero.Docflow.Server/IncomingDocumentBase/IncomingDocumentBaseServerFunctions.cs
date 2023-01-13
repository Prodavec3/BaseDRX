using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.IncomingDocumentBase;

namespace Sungero.Docflow.Server
{
  partial class IncomingDocumentBaseFunctions
  {
    /// <summary>
    /// Заполнить текстовое отображение адресатов.
    /// </summary>
    public virtual void SetManyAddresseesLabel()
    {
      var addressees = _obj.Addressees
        .Where(x => x.Addressee != null)
        .Select(x => x.Addressee)
        .ToList();
      
      var maxLength = _obj.Info.Properties.ManyAddresseesLabel.Length;
      var label = Functions.Module.BuildManyAddresseesLabel(addressees, maxLength);
      if (_obj.ManyAddresseesLabel != label)
        _obj.ManyAddresseesLabel = label;
    }
    
    /// <summary>
    /// Преобразовать документ в PDF с наложением отметки о поступлении в новую версию.
    /// </summary>
    /// <param name="rightIndent">Значение отступа справа.</param>
    /// <param name="bottomIndent">Значение отступа снизу.</param>
    /// <returns>Результат преобразования.</returns>
    [Remote]
    public virtual Structures.OfficialDocument.СonversionToPdfResult AddRegistrationStamp(double rightIndent, double bottomIndent)
    {
      var versionId = _obj.LastVersion.Id;
      var result = Structures.OfficialDocument.СonversionToPdfResult.Create();
      result.HasErrors = true;
      
      // Проверки возможности преобразования и наложения отметки.
      var lastVersionExtension = _obj.LastVersion.AssociatedApplication.Extension.ToLower();
      if (!PublicFunctions.OfficialDocument.CheckPdfConvertibilityByExtension(_obj, lastVersionExtension))
        return Functions.OfficialDocument.GetExtensionValidationError(_obj, lastVersionExtension);
      
      // Выбор способа преобразования.
      var isInteractive = Functions.OfficialDocument.CanConvertToPdfInteractively(_obj);
      if (isInteractive)
      {
        // Способ преобразования: интерактивно.
        var registrationStamp = this.GetRegistrationStampAsHtml();
        result = this.ConvertToPdfAndAddRegistrationStamp(versionId, registrationStamp, rightIndent, bottomIndent);
        result.IsFastConvertion = true;
        result.ErrorTitle = OfficialDocuments.Resources.ConvertionErrorTitleBase;
      }
      else
      {
        var asyncAddRegistrationStamp = Docflow.AsyncHandlers.AddRegistrationStamp.Create();
        asyncAddRegistrationStamp.DocumentId = _obj.Id;
        asyncAddRegistrationStamp.VersionId = versionId;
        asyncAddRegistrationStamp.RightIndent = rightIndent;
        asyncAddRegistrationStamp.BottomIndent = bottomIndent;
        
        var startedNotificationText = OfficialDocuments.Resources.ConvertionInProgress;
        var completedNotificationText = IncomingDocumentBases.Resources.AddRegistrationStampCompleteNotificationFormat(Hyperlinks.Get(_obj));
        asyncAddRegistrationStamp.ExecuteAsync(startedNotificationText, completedNotificationText);
        
        result.IsOnConvertion = true;
        result.HasErrors = false;
      }
      
      Logger.DebugFormat("Registration stamp. Added {5}. Document id - {0}, kind - {6}, format - {1}, application - {2}, right indent - {3}, bottom indent - {4}.",
                         _obj.Id, _obj.AssociatedApplication.Extension, _obj.AssociatedApplication, rightIndent, bottomIndent,
                         isInteractive ? "interactively" : "async", _obj.DocumentKind.DisplayValue);
      
      return result;
    }
    
    /// <summary>
    /// Проверить, что можно сменить тип документа на простой.
    /// </summary>
    /// <returns>True - если можно сменить, иначе - false.</returns>
    [Remote(IsPure = true)]
    public override bool HasSpecifiedTypeRelations()
    {
      var hasSpecifiedTypeRelations = false;
      AccessRights.AllowRead(
        () =>
        {
          hasSpecifiedTypeRelations = OutgoingDocumentBases.GetAll().Any(x => Equals(x.InResponseTo, _obj));
        });
      return base.HasSpecifiedTypeRelations() || hasSpecifiedTypeRelations;
    }
  }
}