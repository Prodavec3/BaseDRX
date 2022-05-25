using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.ExchangeCore.BoxBase;

namespace Sungero.ExchangeCore.Server
{
  partial class BoxBaseFunctions
  {
    /// <summary>
    /// Получить ответственного за ящик.
    /// </summary>
    /// <returns>Ответственный.</returns>
    [Public]
    public virtual Sungero.Company.IEmployee GetResponsible()
    {
      return _obj.Responsible;
    }
    
    /// <summary>
    /// Получить сервис обмена ящика.
    /// </summary>
    /// <returns>Сервис обмена.</returns>
    [Public]
    public virtual IExchangeService GetExchangeService()
    {
      return null;
    }
    
    /// <summary>
    /// Получить НОР ящика.
    /// </summary>
    /// <returns>Наша организация.</returns>
    [Public]
    public virtual Sungero.Company.IBusinessUnit GetBusinessUnit()
    {
      return null;
    }
    
    /// <summary>
    /// Отправлять задания/уведомления ответственному.
    /// </summary>
    /// <returns>Признак отправки задания ответственному за ящик/контрагента.</returns>
    [Public]
    public virtual bool NeedReceiveTask()
    {
      return _obj.Routing != Routing.NoAssignments;
    }
    
    /// <summary>
    /// Получить подразделение ящика.
    /// </summary>
    /// <returns>Подразделение.</returns>
    [Public]
    public virtual Company.IDepartment GetDepartment()
    {
      return null;
    }
    
    /// <summary>
    /// Получить действующие дочерние ящики подразделения.
    /// </summary>
    /// <returns>Список дочерних ящиков подразделений.</returns>
    [Remote]
    public List<IDepartmentBox> GetActiveChildBoxes()
    {
      return Functions.BoxBase.GetChildBoxes(_obj, ExchangeCore.DepartmentBox.Status.Active);
    }
    
    /// <summary>
    /// Получить закрытые дочерние ящики подразделения.
    /// </summary>
    /// <returns>Список дочерних ящиков подразделений.</returns>
    [Remote]
    public List<IDepartmentBox> GetClosedChildBoxes()
    {
      return Functions.BoxBase.GetChildBoxes(_obj, ExchangeCore.DepartmentBox.Status.Closed);
    }
    
    /// <summary>
    /// Получить дочерние ящики подразделения.
    /// </summary>
    /// <param name="status">Статус ящика подразделения.</param>
    /// <returns>Список дочерних ящиков подразделений.</returns>
    public List<IDepartmentBox> GetChildBoxes(Sungero.Core.Enumeration status)
    {
      var resultBoxes = new List<IDepartmentBox>();
      var boxes = DepartmentBoxes.GetAll(b => b.Status == status).ToList();
      var childBoxes = boxes.Where(b => Equals(b.ParentBox, _obj)).ToList();
      while (childBoxes.Any())
      {
        resultBoxes.AddRange(childBoxes);
        var newBoxes = boxes.Where(b => childBoxes.Any(x => Equals(BoxBases.As(x), b.ParentBox))).ToList();
        childBoxes = newBoxes;
      }
      
      return resultBoxes;
    }

    /// <summary>
    /// Получить ответственного за документ.
    /// </summary>
    /// <param name="counterparty">Контрагент.</param>
    /// <param name="infos">Список информаций о документе.</param>
    /// <returns>Ответственный.</returns>
    [Public, Remote]
    public virtual Sungero.Company.IEmployee GetExchangeDocumentResponsible(Sungero.Parties.ICounterparty counterparty, List<Sungero.Exchange.IExchangeDocumentInfo> infos)
    {
      var company = Parties.CompanyBases.As(counterparty);
      if (_obj.Routing == ExchangeCore.BoxBase.Routing.CPResponsible && company != null && company.Responsible != null)
        return company.Responsible;

      return ExchangeCore.PublicFunctions.BoxBase.GetResponsible(_obj);
    }

    /// <summary>
    /// Получить срок задания на обработку.
    /// </summary>
    /// <param name="assignee">Исполнитель задания.</param>
    /// <returns>Срок задания на обработку документа из сервиса обмена.</returns>
    [Public]
    public virtual DateTime GetProcessingTaskDeadline(IEmployee assignee)
    {
      DateTime deadline = Calendar.Now;
      if (_obj.DeadlineInDays.HasValue)
        deadline = deadline.AddWorkingDays(assignee, _obj.DeadlineInDays.Value);
      if (_obj.DeadlineInHours.HasValue)
        deadline = deadline.AddWorkingHours(assignee, _obj.DeadlineInHours.Value);
      if (!_obj.DeadlineInDays.HasValue && !_obj.DeadlineInHours.HasValue)
        deadline = deadline.AddWorkingHours(assignee, Sungero.ExchangeCore.PublicConstants.BoxBase.DefaultDeadlineInHours);
      return deadline;
    }
  }
}