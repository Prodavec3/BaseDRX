  -- Заполнение срока задания DeadlineInHour в абонентском ящике.
if exists(select *
          from information_schema.COLUMNS
          where TABLE_NAME = 'Sungero_ExCore_BoxBase' and COLUMN_NAME = 'Routing')
begin
  UPDATE Sungero_ExCore_BoxBase
  SET DeadlineInHour = 4
  WHERE Routing <> 'NoAssignments' and isnull(DeadlineInDays, 0) = 0 and isnull(DeadlineInHour, 0) = 0
end