USE [Health.MsSqlDatabase]
GO
/****** Object:  Trigger [dbo].[InsertWorkDayOfDoctors]    Script Date: 11/23/2011 10:13:28 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
ALTER TRIGGER [dbo].[InsertWorkDayOfDoctors]
ON [dbo].[WorkWeeks]
AFTER INSERT 
AS 
BEGIN
	IF EXISTS(SELECT*FROM [WorkWeeks] 
	WHERE (SELECT DayInWeek FROM inserted) = WorkWeeks.DayInWeek
	AND
	(SELECT DoctorId FROM inserted) = WorkWeeks.DoctorId
	)
	BEGIN
	ROLLBACK TRAN
	PRINT 'Отмена добавления рабочего расписания'
	END
END