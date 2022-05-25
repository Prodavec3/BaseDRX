using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.CheckReturnCheckAssignment;

namespace Sungero.Docflow.Client
{
  partial class CheckReturnCheckAssignmentActions
  {
    public virtual void NotReturned(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      
    }

    public virtual bool CanNotReturned(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return true;
    }

    public virtual void Returned(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      
    }

    public virtual bool CanReturned(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return true;
    }

    public virtual void NotReturned(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      
    }

    public virtual bool CanNotReturned(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return true;
    }

    public virtual void Returned(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      
    }

    public virtual bool CanReturned(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return true;
    }
  }
}