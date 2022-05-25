using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.Addendum;

namespace Sungero.Docflow.Shared
{
  partial class AddendumFunctions
  {
    public override void FillName()
    {
      if (_obj.DocumentKind != null && !_obj.DocumentKind.GenerateDocumentName.Value && _obj.Name == OfficialDocuments.Resources.DocumentNameAutotext)
        _obj.Name = string.Empty;
      
      if (_obj.DocumentKind == null || !_obj.DocumentKind.GenerateDocumentName.Value)
        return;
      
      var name = string.Empty;
      
      /* Имя в формате:
        <Вид документа> №<номер> "<содержание>" к <имя ведущего документа>.
       */
      using (TenantInfo.Culture.SwitchTo())
      {
        if (!string.IsNullOrWhiteSpace(_obj.RegistrationNumber))
          name += OfficialDocuments.Resources.Number + _obj.RegistrationNumber;
        
        if (!string.IsNullOrEmpty(_obj.Subject))
          name += " " + _obj.Subject;
        
        if (_obj.LeadingDocument != null)
        {
          name += OfficialDocuments.Resources.NamePartForLeadDocument;
          name += Functions.Module.ReplaceFirstSymbolToLowerCase(GetDocumentName(_obj.LeadingDocument));
        }
      }
      
      if (string.IsNullOrWhiteSpace(name))
        name = OfficialDocuments.Resources.DocumentNameAutotext;
      else if (_obj.DocumentKind != null)
        name = _obj.DocumentKind.ShortName + name;
      
      name = Docflow.PublicFunctions.Module.TrimSpecialSymbols(name);
      
      _obj.Name = Docflow.PublicFunctions.OfficialDocument.AddClosingQuote(name, _obj);
      
    }
    
    /// <summary>
    /// Получить наименование документа.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>Наименование документа.</returns>
    public static string GetDocumentName(IOfficialDocument document)
    {
      return document.AccessRights.CanRead() ? document.Name : Functions.Addendum.Remote.GetLeadingDocument(document.Id).Name;
    }
    
    public override void RefreshDocumentForm()
    {
      base.RefreshDocumentForm();
      
      var isNotNumerable = _obj.DocumentKind == null || _obj.DocumentKind.NumberingType == Docflow.DocumentKind.NumberingType.NotNumerable;
      _obj.State.Properties.BusinessUnit.IsVisible = !isNotNumerable;
      _obj.State.Properties.Department.IsVisible = !isNotNumerable;
      _obj.State.Properties.OurSignatory.IsVisible = !isNotNumerable;
      _obj.State.Properties.PreparedBy.IsVisible = !isNotNumerable;
      _obj.State.Properties.Assignee.IsVisible = !isNotNumerable;
    }
    
    public override void ChangeDocumentPropertiesAccess(bool isEnabled, bool isRepeatRegister)
    {
      base.ChangeDocumentPropertiesAccess(isEnabled, isRepeatRegister);
      
      _obj.State.Properties.LeadingDocument.IsEnabled = isEnabled || isRepeatRegister;
    }
    
    public override string GetLeadDocumentNumber()
    {
      var doc = _obj.LeadingDocument;
      return doc.AccessRights.CanRead() ? doc.RegistrationNumber : Functions.Addendum.Remote.GetLeadingDocument(doc.Id).Number;
    }
    
    public override void ChangeRegistrationPaneVisibility(bool needShow, bool repeatRegister)
    {
      base.ChangeRegistrationPaneVisibility(needShow, repeatRegister);
      
      var notNumerable = _obj.DocumentKind != null && _obj.DocumentKind.NumberingType == Docflow.DocumentKind.NumberingType.NotNumerable;
      var needShowRegistrationProperties = !notNumerable && needShow;
      var canRegister = _obj.AccessRights.CanRegister();
      var caseIsEnabled = notNumerable || !notNumerable && canRegister;
      // Может быть уже закрыто от редактирования, если документ зарегистрирован и в формате номера журнала
      // присутствует индекс файла.
      caseIsEnabled = caseIsEnabled && _obj.State.Properties.CaseFile.IsEnabled;
      
      _obj.State.Properties.RegistrationNumber.IsVisible = needShowRegistrationProperties;
      _obj.State.Properties.RegistrationDate.IsVisible = needShowRegistrationProperties;
      _obj.State.Properties.CaseFile.IsEnabled = caseIsEnabled;
      _obj.State.Properties.PlacedToCaseFileDate.IsEnabled = caseIsEnabled;
    }
    
    public override bool IsProjectDocument()
    {
      var isProject = base.IsProjectDocument();
      return isProject ? isProject : Functions.OfficialDocument.IsProjectDocument(_obj.LeadingDocument);
    }
    
    public override IProjectBase GetProject()
    {
      return base.GetProject() ?? Functions.OfficialDocument.GetProject(_obj.LeadingDocument);
    }
  }
}