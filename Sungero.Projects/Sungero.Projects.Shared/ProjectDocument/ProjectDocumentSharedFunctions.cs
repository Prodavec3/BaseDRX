using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Projects.ProjectDocument;

namespace Sungero.Projects.Shared
{
  partial class ProjectDocumentFunctions
  {
    public override void FillName()
    {
      var documentKind = _obj.DocumentKind;
      
      if (documentKind != null && !documentKind.GenerateDocumentName.Value && _obj.Name == Docflow.Resources.DocumentNameAutotext)
        _obj.Name = string.Empty;
      
      if (documentKind == null || !documentKind.GenerateDocumentName.Value)
        return;
      
      /*
        Имя в формате: <Имя проекта>. <Вид документа> №<номер> от <дата> <содержание>.
        
        Содержание специально без кавычек.
       */
      using (TenantInfo.Culture.SwitchTo())
      {
        var name = string.Empty;
        
        var project = Projects.As(_obj.Project);
        if (project != null)
          name += project.ShortName + ". ";
        
        if (!string.IsNullOrWhiteSpace(_obj.RegistrationNumber))
          name += Sungero.Docflow.OfficialDocuments.Resources.Number + _obj.RegistrationNumber;
        
        if (_obj.RegistrationDate != null)
          name += Sungero.Docflow.OfficialDocuments.Resources.DateFrom + _obj.RegistrationDate.Value.ToString("d");
        
        if (!string.IsNullOrWhiteSpace(_obj.Subject))
          name += " " + _obj.Subject;
                
        if (string.IsNullOrWhiteSpace(name))
          name = Docflow.Resources.DocumentNameAutotext;
        else if (documentKind != null)
          name = documentKind.DisplayValue + " " + name;
        
        name = Docflow.PublicFunctions.Module.TrimSpecialSymbols(name);
        
        _obj.Name = Docflow.PublicFunctions.OfficialDocument.AddClosingQuote(name, _obj);
      }
    }
    
    public override void RefreshDocumentForm()
    {
      base.RefreshDocumentForm();

      var isNotNumerable = _obj.DocumentKind == null || _obj.DocumentKind.NumberingType == Docflow.DocumentKind.NumberingType.NotNumerable;
      _obj.State.Properties.BusinessUnit.IsVisible = !isNotNumerable;
      _obj.State.Properties.Department.IsVisible = !isNotNumerable;
      _obj.State.Properties.OurSignatory.IsVisible = !isNotNumerable || this.GetShowOurSigningReasonParam();
      _obj.State.Properties.PreparedBy.IsVisible = !isNotNumerable;
      _obj.State.Properties.Assignee.IsVisible = !isNotNumerable;
    }
    
    public override void SetRequiredProperties()
    {
      base.SetRequiredProperties();
      
      _obj.State.Properties.Subject.IsRequired = false;
    }
    
    public override bool NeedClearProject(Sungero.Docflow.Shared.OfficialDocumentDocumentKindChangedEventArgs e)
    {
      return false;
    }
    
    [Obsolete("Используйте метод GetDefaultSignatory().")]
    public override Sungero.Company.IEmployee GetDefaultSignatory(List<Docflow.Structures.SignatureSetting.Signatory> signatories)
    {
      if (Company.Employees.Current != null)
      {
        var businessUnit = Company.PublicFunctions.BusinessUnit.Remote.GetBusinessUnit(Company.Employees.Current);
        var ceo = businessUnit != null ? businessUnit.CEO : null;
        if (ceo != null && signatories.Any(s => Equals(s.EmployeeId, ceo.Id)))
          return ceo;
      }
      return base.GetDefaultSignatory(signatories);
    }
    
  }
}