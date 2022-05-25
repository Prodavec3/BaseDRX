do $$
 declare defaultBusinessUnit integer;
begin
defaultBusinessUnit := (
  select min(id)
  from sungero_core_recipient
  where discriminator = 'EFF95720-181F-4F7D-892D-DEC034C7B2AB' --Дискриминатор для типа Business unit
    and status = 'Active'
);
if (defaultBusinessUnit is not null)
then
  with recursive Departments_CTE(id, name, headoffice_company_sungero, businessunit_company_sungero) as
  (
    select id, name, headoffice_company_sungero, businessunit_company_sungero
    from sungero_core_recipient
    where headoffice_company_sungero is null
      and discriminator = '61B1C19F-26E2-49A5-B3D3-0D3618151E12' --Дискриминатор для типа Department
    union all
    select recipient.id, recipient.name, recipient.headoffice_company_sungero,
      coalesce(recipient.businessunit_company_sungero, dep.businessunit_company_sungero)
    from sungero_core_recipient as recipient
      inner join Departments_CTE as dep on recipient.headoffice_company_sungero = dep.id
    where recipient.headoffice_company_sungero is not null
      and discriminator = '61B1C19F-26E2-49A5-B3D3-0D3618151E12' --Дискриминатор для типа Department
  )
 
  update sungero_docflow_caseFile
  set businessunit = coalesce(dep.businessunit_company_sungero, defaultBusinessUnit)
  from sungero_docflow_casefile cases
    left join Departments_CTE dep on cases.department = dep.id
  where cases.businessunit is null;

end if;
end $$;