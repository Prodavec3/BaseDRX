using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.FormalizedPowerOfAttorney;

namespace Sungero.Docflow.Client
{
  partial class FormalizedPowerOfAttorneyFunctions
  {
    /// <summary>
    /// Показать диалог импорта эл. доверенности с подписью из файла.
    /// </summary>
    /// <returns>True, если импорт прошел успешно, иначе - false.</returns>
    [Public]
    public virtual bool ShowImportVersionWithSignatureDialog()
    {
      const string XmlExtension = "xml";
      var dialog = Dialogs.CreateInputDialog(FormalizedPowerOfAttorneys.Resources.ImportPowerOfAttorneyFromXml);
      var fileSelector = dialog.AddFileSelect(FormalizedPowerOfAttorneys.Resources.File, true);
      fileSelector.WithFilter(string.Empty, XmlExtension);
      var signatureSelector = dialog.AddFileSelect(FormalizedPowerOfAttorneys.Resources.SignatureFile, true);
      signatureSelector.WithFilter(string.Empty, "sgn", "sig");
      var importButton = dialog.Buttons.AddCustom(FormalizedPowerOfAttorneys.Resources.Import);
      dialog.HelpCode = Constants.FormalizedPowerOfAttorney.ImportFromXmlHelpCode;
      dialog.Buttons.AddCancel();
      
      // Добавление версии документа выполняется в клиентском коде,
      // чтобы версия сразу появилась в списке версий без дополнительного обновления карточки.
      Sungero.Content.IElectronicDocumentVersions version = null;
      dialog.SetOnButtonClick(
        b =>
        {
          if (b.Button == importButton && b.IsValid)
          {
            try
            {
              // Импорт тела, заполнение свойств и импорт подписи выполняются в одной Remote-функции,
              // чтобы при ошибке на любом из этапов откатывалось всё остальное.
              // Создание версии выполняется на клиенте для корректного обновления карточки после импорта.
              var xml = Docflow.Structures.Module.ByteArray.Create(fileSelector.Value.Content);
              var signature = Docflow.Structures.Module.ByteArray.Create(signatureSelector.Value.Content);
              version = _obj.Versions.AddNew();
              version.AssociatedApplication = Content.AssociatedApplications.GetByExtension(XmlExtension);
              
              // Перейти в невизуальный режим для возможности сохранения (сохранение необходимо для импорта подписи).
              // Визуальный режим и обязательность полей восстановятся после выполнения действия на рефреше.
              ((Domain.Shared.IExtendedEntity)_obj).Params.Remove(Sungero.Docflow.PublicConstants.OfficialDocument.IsVisualModeParamName);
              Functions.OfficialDocument.SetRequiredProperties(_obj);
              
              // Импорт тела и подписи.
              Functions.FormalizedPowerOfAttorney.Remote.ImportFormalizedPowerOfAttorneyFromXmlAndSign(_obj, xml, signature);
            }
            catch (AppliedCodeException ax)
            {
              if (version != null)
                _obj.Versions.Remove(version);
              Logger.Error(ax.Message, ax);
              b.AddError(ax.Message);
            }
            catch (Exception ex)
            {
              if (version != null)
                _obj.Versions.Remove(version);
              var error = FormalizedPowerOfAttorneys.Resources.FormalizedPowerOfAttorneyImportFailed;
              Logger.Error(error, ex);
              b.AddError(string.Format("{0} {1}", error, ex.Message), fileSelector);
            }
          }
        });
      
      if (dialog.Show() == importButton)
      {
        Dialogs.NotifyMessage(FormalizedPowerOfAttorneys.Resources.PowerOfAttorneySuccessfullyImported);
        return true;
      }
      
      return false;
    }
  }
}