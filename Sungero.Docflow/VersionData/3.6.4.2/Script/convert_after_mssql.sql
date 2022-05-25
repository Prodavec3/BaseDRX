declare @defaultBusinessUnit int

set @defaultBusinessUnit = (
  select min(Id)
  from Sungero_Core_Recipient
  where Discriminator = 'EFF95720-181F-4F7D-892D-DEC034C7B2AB' --Дискриминатор для типа Business unit
    and [Status] = 'Active'
)

if (@defaultBusinessUnit is not null)
begin

  ;with Departments_CTE as 
  (
    select Id, Name, HeadOffice_Company_Sungero, BusinessUnit_Company_Sungero
    from Sungero_Core_Recipient
    where HeadOffice_Company_Sungero is null
      and Discriminator = '61B1C19F-26E2-49A5-B3D3-0D3618151E12' --Дискриминатор для типа Department
    union all
    select Recipient.Id, Recipient.Name, Recipient.HeadOffice_Company_Sungero, 
      isnull(Recipient.BusinessUnit_Company_Sungero, Dep.BusinessUnit_Company_Sungero)
    from Sungero_Core_Recipient as Recipient
      inner join Departments_CTE as Dep on Recipient.HeadOffice_Company_Sungero = Dep.Id
    where Recipient.HeadOffice_Company_Sungero is not null
      and Discriminator = '61B1C19F-26E2-49A5-B3D3-0D3618151E12' --Дискриминатор для типа Department
  )

  update Cases
  set BusinessUnit = isnull(Dep.BusinessUnit_Company_Sungero, @defaultBusinessUnit)
  from Sungero_Docflow_CaseFile Cases
    left join Departments_CTE Dep on Cases.Department = Dep.Id
  where Cases.BusinessUnit is null

end