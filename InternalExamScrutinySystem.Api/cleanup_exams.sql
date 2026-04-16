BEGIN TRANSACTION;

-- 1. Delete dependent Scrutiny data
DELETE FROM ScrutinyReports;
DELETE FROM ScrutinyAssignments;

-- 2. Delete Question Papers
DELETE FROM QuestionPapers;

-- 3. Delete Exam-Subject mappings
DELETE FROM ExamSubjects;

-- 4. Delete Faculty-Subject-Exam assignments
DELETE FROM FacultySubjectAssignments;

-- 5. Delete relevant notifications
DELETE FROM Notifications;

-- 6. Delete the Exams themselves
DELETE FROM Exams;

COMMIT TRANSACTION;
GO

-- Verify Counts
SELECT 'Exams' AS T, COUNT(*) AS C FROM Exams
UNION ALL
SELECT 'ExamSubjects', COUNT(*) FROM ExamSubjects
UNION ALL
SELECT 'QuestionPapers', COUNT(*) FROM QuestionPapers
UNION ALL
SELECT 'ScrutinyReports', COUNT(*) FROM ScrutinyReports
UNION ALL
SELECT 'Notifications', COUNT(*) FROM Notifications;
GO
