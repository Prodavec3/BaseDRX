do $$
begin

  if (exists (select 1 from information_schema.Columns where table_name = 'sungero_docregister_currentnumbers')
      and not exists (select 1 from information_schema.Columns where table_name = 'sungero_docregister_currentnumbers' and column_name = 'day'))
  then
    alter table Sungero_DocRegister_CurrentNumbers
    add Day integer not null default(0);
  end if;

end $$