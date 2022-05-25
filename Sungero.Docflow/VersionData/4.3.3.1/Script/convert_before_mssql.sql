if (exists (select * from information_schema.columns where table_name = 'Sungero_DocRegister_CurrentNumbers')
    and not exists (select * from information_schema.columns where table_name = 'Sungero_DocRegister_CurrentNumbers' and column_name = 'Day'))
begin
  alter table dbo.Sungero_DocRegister_CurrentNumbers
  add Day integer not null default(0)
end