do $$
begin
-- Заполнение сроа задания DeadlineInHour в абонентском ящике.
if exists(select *
          from information_schema.COLUMNS
          where TABLE_NAME = 'sungero_excore_boxbase' and COLUMN_NAME = 'routing')
then
  UPDATE sungero_excore_boxbase
  SET DeadlineInHour = 4
  WHERE routing <> 'NoAssignments' and coalesce(DeadlineInDays, 0) = 0 and coalesce(DeadlineInHour, 0) = 0;
end if;
end $$;