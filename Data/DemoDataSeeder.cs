using EmployeeSelfServiceAPI.Domain.Entities.Common;
using EmployeeSelfServiceAPI.Domain.Entities.Identity;
using EmployeeSelfServiceAPI.Domain.Entities.Learning;
using EmployeeSelfServiceAPI.Domain.Entities.Opportunities;
using EmployeeSelfServiceAPI.Domain.Entities.Payroll;
using EmployeeSelfServiceAPI.Domain.Entities.Performance;
using EmployeeSelfServiceAPI.Domain.Entities.Training;
using EmployeeSelfServiceAPI.Domain.Enums;
using EmployeeSelfServiceAPI.Helpers;
using Microsoft.EntityFrameworkCore;

namespace EmployeeSelfServiceAPI.Data;

// Realistic demo data for every module, in states the business rules allow, with matching history.
// - Each module is seeded once, detected by a marker (seed references / titles), not by an empty table.
// - Dates are relative to today (Lagos), so "after training end", "overdue" and "due tomorrow" demos always work.
// - No EmailMessage rows are created: seeded notifications are in-app only, nothing is mailed to @ess.test.
public static class DemoDataSeeder
{
    private static readonly IAppClock Clock = new AppClock();

    public static async Task SeedAsync(AppDbContext db)
    {
        var people = await db.Employees
            .Include(e => e.Grade)
            .Include(e => e.Department)
            .Where(e => e.EmployeeNumber.StartsWith("ESS-00"))
            .ToDictionaryAsync(e => e.EmployeeNumber);

        // Needs the standard seeded accounts; skip quietly if someone changed them
        string[] required = ["ESS-0001", "ESS-0002", "ESS-0003", "ESS-0004", "ESS-0005", "ESS-0006", "ESS-0007", "ESS-0008", "ESS-0009", "ESS-0010"];
        if (required.Any(n => !people.ContainsKey(n))) return;

        var s = new Cast(people);

        await SeedTrainingAsync(db, s);
        await SeedLearningAsync(db, s);
        await SeedPerformanceAsync(db, s);
        await SeedPayrollAsync(db, s);
        await SeedOpportunitiesAsync(db, s);
    }

    // The seeded people, by role in the demo
    private sealed class Cast(Dictionary<string, Employee> p)
    {
        public Employee Tunde { get; } = p["ESS-0001"];    // Admin, Manager
        public Employee Ngozi { get; } = p["ESS-0002"];    // HR, Manager
        public Employee Bola { get; } = p["ESS-0003"];     // HR
        public Employee Chinedu { get; } = p["ESS-0004"];  // Finance, Manager
        public Employee Amina { get; } = p["ESS-0005"];    // Finance
        public Employee Ifeoma { get; } = p["ESS-0006"];   // Manager (Engineering)
        public Employee Musa { get; } = p["ESS-0007"];     // Manager (Operations)
        public Employee Kunle { get; } = p["ESS-0008"];    // Staff, GL-08, Engineering
        public Employee Zainab { get; } = p["ESS-0009"];   // Staff, GL-10, Engineering
        public Employee Emeka { get; } = p["ESS-0010"];    // Staff, GL-08, Operations
    }

    // ------------------------------------------------------------------ Training

    private static async Task SeedTrainingAsync(AppDbContext db, Cast s)
    {
        if (await db.TrainingRequests.AnyAsync(t => t.Reference.StartsWith("TRN-SEED-"))) return;

        var today = Clock.Today;
        var now = Clock.UtcNow;
        var rates = await db.EstacodeRates.Where(r => r.IsActive).ToListAsync();
        var seq = 0;

        TrainingRequest Request(Employee owner, string country, string city, string title, string provider,
            DateOnly start, DateOnly end, int travelBuffer = 0)
        {
            var created = now.AddDays(-30);
            var request = new TrainingRequest
            {
                Reference = $"TRN-SEED-{++seq:000}",
                Employee = owner,
                Title = title,
                Provider = provider,
                CountryCode = country,
                City = city,
                TrainingStartDate = start,
                TrainingEndDate = end,
                DepartureDate = start.AddDays(-travelBuffer),
                ReturnDate = end.AddDays(travelBuffer),
                BusinessJustification = $"{title} builds skills the team needs for upcoming projects.",
                CourseFee = 1500m,
                CourseFeeCurrency = "USD",
                FlightEstimate = 850000m,
                FlightEstimateCurrency = "NGN",
                Status = TrainingStatus.Draft,
                CreatedAt = created,
                UpdatedAt = created
            };
            History(request, TrainingAction.Created, null, TrainingStatus.Draft, owner, null, created);
            return request;
        }

        void History(TrainingRequest r, TrainingAction action, TrainingStatus? from, TrainingStatus to, Employee actor, string? comment, DateTime at) =>
            r.StatusHistory.Add(new TrainingStatusHistory { Action = action, FromStatus = from, ToStatus = to, Actor = actor, Comment = comment, CreatedAt = at });

        void Move(TrainingRequest r, TrainingAction action, TrainingStatus to, Employee actor, string? comment, int daysAgo)
        {
            var at = now.AddDays(-daysAgo);
            History(r, action, r.Status, to, actor, comment, at);
            r.Status = to;
            r.UpdatedAt = at;
        }

        void Submit(TrainingRequest r, Employee reviewer, int daysAgo)
        {
            r.AssignedReviewer = reviewer;
            r.SubmittedAt = now.AddDays(-daysAgo);
            Move(r, TrainingAction.Submitted, TrainingStatus.PendingManagerApproval, r.Employee!, null, daysAgo);
        }

        EstacodeAllowance Allowance(TrainingRequest r, Employee hr, int daysAgo)
        {
            var rate = rates.First(x => x.CountryCode == r.CountryCode && x.GradeId == r.Employee!.GradeId);
            var days = AllowanceCalculator.EligibleDays(r.DepartureDate, r.ReturnDate);
            var allowance = new EstacodeAllowance
            {
                VersionNumber = 1,
                GradeCode = r.Employee!.Grade!.Code,
                CountryCode = r.CountryCode,
                DailyRate = rate.DailyRate,
                Currency = rate.Currency,
                ComputedEligibleDays = days,
                EligibleDays = days,
                Amount = AllowanceCalculator.Amount(days, rate.DailyRate),
                CreatedBy = hr,
                CreatedAt = now.AddDays(-daysAgo)
            };
            r.Allowances.Add(allowance);
            Move(r, TrainingAction.HRApproved, TrainingStatus.PendingFinanceApproval, hr,
                $"Allowance v1: {days} days x {rate.Currency} {rate.DailyRate:N2} = {allowance.Amount:N2}", daysAgo);
            return allowance;
        }

        // 1. Draft, still editable by Kunle
        var draft = Request(s.Kunle, "GB", "London", "Advanced Cloud Architecture", "Cloud Academy UK", today.AddDays(30), today.AddDays(34));

        // 2. Waiting for Ifeoma (manager approve / reject / reassign demo)
        var pendingManager = Request(s.Zainab, "US", "New York", "Kubernetes in Production", "CNCF Training", today.AddDays(40), today.AddDays(44), 1);
        Submit(pendingManager, s.Ifeoma, 3);

        // 3. Waiting for HR (HR approve demo)
        var pendingHr = Request(s.Emeka, "ZA", "Johannesburg", "Supply Chain Operations", "SA Logistics Institute", today.AddDays(20), today.AddDays(23));
        Submit(pendingHr, s.Musa, 6);
        Move(pendingHr, TrainingAction.ManagerApproved, TrainingStatus.PendingHRApproval, s.Musa, "Relevant to the Q1 logistics plan.", 4);

        // 4. Japan has no estacode rate: HR approval returns 409 naming JP and the grade
        var noRate = Request(s.Kunle, "JP", "Tokyo", "Embedded Systems Summit", "JEITA", today.AddDays(50), today.AddDays(54));
        Submit(noRate, s.Ifeoma, 5);
        Move(noRate, TrainingAction.ManagerApproved, TrainingStatus.PendingHRApproval, s.Ifeoma, "Approved; please confirm the allowance.", 4);

        // 5. Waiting for Finance with allowance v1 = 5 days x USD 150 = 750.00 (the TRN-03 worked example)
        var pendingFinance = Request(s.Kunle, "KE", "Nairobi", "Data Engineering Bootcamp", "Moringa School", today.AddDays(25), today.AddDays(29));
        Submit(pendingFinance, s.Ifeoma, 10);
        Move(pendingFinance, TrainingAction.ManagerApproved, TrainingStatus.PendingHRApproval, s.Ifeoma, null, 9);
        Allowance(pendingFinance, s.Bola, 7);

        // 6. Returned to HR by Finance (correction demo: HR submits version 2)
        var returned = Request(s.Kunle, "US", "Seattle", "Distributed Systems Workshop", "O'Reilly", today.AddDays(60), today.AddDays(63), 1);
        Submit(returned, s.Ifeoma, 12);
        Move(returned, TrainingAction.ManagerApproved, TrainingStatus.PendingHRApproval, s.Ifeoma, null, 11);
        Allowance(returned, s.Bola, 9);
        Move(returned, TrainingAction.FinanceReturnedToHR, TrainingStatus.ReturnedToHR, s.Amina,
            "The workshop ends at noon on the last day; reduce eligible days by one.", 8);

        // 7. Approved, training ended 10 days ago: Zainab can upload evidence and download the PDF
        var approved = Request(s.Zainab, "AE", "Dubai", "Product Security Conference", "GITEX", today.AddDays(-14), today.AddDays(-10), 1);
        Submit(approved, s.Ifeoma, 40);
        Move(approved, TrainingAction.ManagerApproved, TrainingStatus.PendingHRApproval, s.Ifeoma, null, 38);
        var approvedAllowance = Allowance(approved, s.Bola, 36);
        approvedAllowance.ApprovedBy = s.Amina;
        approvedAllowance.ApprovedAt = now.AddDays(-35);
        Move(approved, TrainingAction.FinanceApproved, TrainingStatus.Approved, s.Amina, null, 35);

        // 8. Approved, ended 75 days ago, still no evidence: appears in HR's 60-day follow-up list
        var overdue = Request(s.Emeka, "GB", "Manchester", "Lean Operations Practitioner", "Lean Institute", today.AddDays(-79), today.AddDays(-75));
        Submit(overdue, s.Musa, 110);
        Move(overdue, TrainingAction.ManagerApproved, TrainingStatus.PendingHRApproval, s.Musa, null, 108);
        var overdueAllowance = Allowance(overdue, s.Bola, 106);
        overdueAllowance.ApprovedBy = s.Amina;
        overdueAllowance.ApprovedAt = now.AddDays(-105);
        Move(overdue, TrainingAction.FinanceApproved, TrainingStatus.Approved, s.Amina, null, 105);

        // 9. Rejected by the manager
        var rejected = Request(s.Zainab, "GB", "London", "General Leadership Retreat", "Retreats Ltd", today.AddDays(15), today.AddDays(17));
        Submit(rejected, s.Ifeoma, 20);
        Move(rejected, TrainingAction.ManagerRejected, TrainingStatus.Rejected, s.Ifeoma,
            "Not aligned with this quarter's engineering priorities.", 18);

        // 10. Withdrawn by the employee
        var withdrawn = Request(s.Emeka, "KE", "Mombasa", "Port Operations Seminar", "KPA Academy", today.AddDays(35), today.AddDays(36));
        Submit(withdrawn, s.Musa, 15);
        Move(withdrawn, TrainingAction.Withdrawn, TrainingStatus.Withdrawn, s.Emeka, "The seminar was moved to next year.", 14);

        db.TrainingRequests.AddRange(draft, pendingManager, pendingHr, noRate, pendingFinance, returned, approved, overdue, rejected, withdrawn);

        Notify(db, s.Ifeoma, "Training request awaiting your review",
            $"Zainab Yusuf submitted {pendingManager.Reference} (Kubernetes in Production) for your approval.", nameof(TrainingRequest), now.AddDays(-3));
        Notify(db, s.Zainab, "Training allowance approved",
            $"Finance approved {approved.Reference}. Your allowance is confirmed and the statement PDF is available.", nameof(TrainingRequest), now.AddDays(-35));

        await db.SaveChangesAsync();
    }

    // ------------------------------------------------------------------ Learning

    private static async Task SeedLearningAsync(AppDbContext db, Cast s)
    {
        const string marker = "Information Security Essentials";
        if (await db.Courses.AnyAsync(c => c.Title == marker)) return;

        var today = Clock.Today;
        var now = Clock.UtcNow;

        Course NewCourse(string title, string description, int passMark, CourseStatus status,
            (string Title, string Content)[] lessons, (string Text, string[] Options, int Correct)[] questions)
        {
            var course = new Course
            {
                Title = title,
                Description = description,
                PassMark = passMark,
                Status = status,
                CreatedBy = s.Bola,
                CreatedAt = now.AddDays(-45),
                UpdatedAt = now.AddDays(-40),
                PublishedAt = status == CourseStatus.Published ? now.AddDays(-40) : null
            };
            var order = 1;
            foreach (var (lessonTitle, content) in lessons)
                course.Lessons.Add(new Lesson { Title = lessonTitle, Content = content, SortOrder = order++ });

            order = 1;
            foreach (var (text, options, correct) in questions)
            {
                var question = new Question { Text = text, SortOrder = order++ };
                for (var i = 0; i < options.Length; i++)
                    question.Options.Add(new QuestionOption { Text = options[i], IsCorrect = i == correct, SortOrder = i + 1 });
                course.Questions.Add(question);
            }
            return course;
        }

        var security = NewCourse(marker,
            "How to recognise and report common security threats at work.", 70, CourseStatus.Published,
            [
                ("Recognising phishing", "Check the sender, hover over links, and never share credentials by email."),
                ("Passwords and MFA", "Use a password manager and turn on multi-factor authentication everywhere."),
                ("Reporting incidents", "Report suspected incidents to the security desk within one hour.")
            ],
            [
                ("What is the safest response to an unexpected password-reset email?", ["Click the link quickly", "Report it and reset via the official site", "Forward it to colleagues"], 1),
                ("Which adds the most protection to an account?", ["A longer username", "Multi-factor authentication", "Changing the password weekly"], 1),
                ("How soon should a suspected incident be reported?", ["Within one hour", "At the end of the week", "Only if data was lost"], 0),
                ("Where should work passwords be stored?", ["A sticky note", "A password manager", "A shared spreadsheet"], 1)
            ]);

        var conduct = NewCourse("Code of Conduct 2026",
            "Our standards for integrity, respect and conflicts of interest.", 80, CourseStatus.Published,
            [
                ("Integrity at work", "Act honestly and declare conflicts of interest."),
                ("Respectful workplace", "Harassment of any kind is never acceptable.")
            ],
            [
                ("A supplier offers you an expensive gift. What do you do?", ["Accept it", "Declare it to your manager", "Ignore it"], 1),
                ("Who can you report misconduct to?", ["Only your manager", "HR or the ethics line", "No one"], 1)
            ]);

        var draft = NewCourse("Customer Service Excellence",
            "Draft course: still being written by HR.", 75, CourseStatus.Draft,
            [("Listening to customers", "Active listening techniques.")],
            [("What is active listening?", ["Waiting to speak", "Giving full attention and confirming understanding"], 1)]);

        db.Courses.AddRange(security, conduct, draft);

        CourseAssignment Assign(Course course, Employee employee, int dueInDays, AssignmentStatus status, int lessonsDone)
        {
            var assignment = new CourseAssignment
            {
                Course = course,
                Employee = employee,
                AssignedBy = s.Bola,
                DueDate = today.AddDays(dueInDays),
                Status = status,
                CurrentCycle = 1,
                CreatedAt = now.AddDays(-30),
                UpdatedAt = now.AddDays(-2)
            };
            foreach (var lesson in course.Lessons.OrderBy(l => l.SortOrder).Take(lessonsDone))
                assignment.LessonProgress.Add(new LessonProgress { Lesson = lesson, CompletedAt = now.AddDays(-5) });
            return assignment;
        }

        void Attempt(CourseAssignment assignment, int attemptNumber, int correctCount, int daysAgo)
        {
            var course = assignment.Course!;
            var questions = course.Questions.OrderBy(q => q.SortOrder).ToList();
            var attempt = new AssessmentAttempt
            {
                CycleNumber = assignment.CurrentCycle,
                AttemptNumber = attemptNumber,
                CorrectCount = correctCount,
                TotalQuestions = questions.Count,
                Score = AssessmentScorer.Score(correctCount, questions.Count),
                SubmittedAt = now.AddDays(-daysAgo)
            };
            attempt.IsPassed = AssessmentScorer.IsPass(attempt.Score, course.PassMark);
            for (var i = 0; i < questions.Count; i++)
            {
                var isCorrect = i < correctCount;
                var option = questions[i].Options.First(o => o.IsCorrect == isCorrect);
                attempt.Answers.Add(new AttemptAnswer { Question = questions[i], SelectedOption = option, IsCorrect = isCorrect });
            }
            assignment.Attempts.Add(attempt);
        }

        var lessonCount = security.Lessons.Count;
        var kunle = Assign(security, s.Kunle, 14, AssignmentStatus.NotStarted, 0);           // fresh: complete lessons, then pass
        var zainab = Assign(security, s.Zainab, 1, AssignmentStatus.InProgress, 1);         // due tomorrow: reminder demo
        var ifeoma = Assign(security, s.Ifeoma, -5, AssignmentStatus.InProgress, 2);         // overdue: dashboard + reminder
        var emeka = Assign(security, s.Emeka, 10, AssignmentStatus.Completed, lessonCount);  // passed 4/4
        Attempt(emeka, 1, 4, 3);
        emeka.CompletedAt = now.AddDays(-3);
        var musa = Assign(security, s.Musa, 7, AssignmentStatus.AttemptsExhausted, lessonCount); // 3 failures: HR reset demo
        Attempt(musa, 1, 1, 6);
        Attempt(musa, 2, 2, 5);
        Attempt(musa, 3, 2, 4);
        var kunleConduct = Assign(conduct, s.Kunle, 7, AssignmentStatus.InProgress, 1);

        db.CourseAssignments.AddRange(kunle, zainab, ifeoma, emeka, musa, kunleConduct);

        Notify(db, s.Kunle, "New course assigned",
            $"You have been assigned \"{marker}\". Complete it by {today.AddDays(14):dd MMM yyyy}.", nameof(Course), now.AddDays(-30));
        Notify(db, s.Musa, "Course attempts used",
            $"All attempts for \"{marker}\" are used. HR can reset the assignment.", nameof(Course), now.AddDays(-4));

        await db.SaveChangesAsync();
    }

    // ------------------------------------------------------------------ Performance (KPI)

    private static async Task SeedPerformanceAsync(AppDbContext db, Cast s)
    {
        const string h2 = "H2 2026 Performance Review";
        if (await db.ReviewPeriods.AnyAsync(p => p.Name == h2)) return;

        var today = Clock.Today;
        var now = Clock.UtcNow;

        // Current period: deadline 60 days out. Previous period: deadline passed 9 days ago.
        var current = new ReviewPeriod
        {
            Name = h2, StartDate = new DateOnly(today.Year, 7, 1), EndDate = today.AddDays(45),
            SubmissionDeadline = today.AddDays(60), CreatedBy = s.Bola, CreatedAt = now.AddDays(-60)
        };
        var previous = new ReviewPeriod
        {
            Name = "Q3 2026 Quarterly Review", StartDate = today.AddDays(-120), EndDate = today.AddDays(-20),
            SubmissionDeadline = today.AddDays(-9), CreatedBy = s.Bola, CreatedAt = now.AddDays(-120)
        };
        db.ReviewPeriods.AddRange(current, previous);

        KpiPlan Plan(Employee owner, ReviewPeriod period, Employee? reviewer, KpiPlanStatus status,
            (string Title, string Unit, decimal Target, decimal Weight, decimal? Actual)[] items)
        {
            var created = period.CreatedAt.AddDays(2);
            var plan = new KpiPlan
            {
                Employee = owner,
                ReviewPeriod = period,
                Status = KpiPlanStatus.Draft,
                EffectiveDeadline = period.SubmissionDeadline,
                CreatedBy = s.Bola,
                CreatedAt = created,
                UpdatedAt = created
            };
            plan.StatusHistory.Add(new KpiPlanStatusHistory
                { Action = KpiAction.Created, ToStatus = KpiPlanStatus.Draft, Actor = s.Bola, CreatedAt = created });

            var order = 1;
            foreach (var (title, unit, target, weight, actual) in items)
                plan.Items.Add(new KpiItem { Title = title, Unit = unit, Target = target, Weight = weight, ActualValue = actual, SortOrder = order++ });

            if (status != KpiPlanStatus.Draft)
            {
                plan.AssignedReviewer = reviewer;
                plan.ActivatedBy = s.Bola;
                plan.ActivatedAt = created.AddDays(1);
                Step(plan, KpiAction.Activated, KpiPlanStatus.Active, s.Bola, $"Reviewer: {reviewer!.FirstName} {reviewer.LastName}", plan.ActivatedAt.Value);
            }
            if (status is KpiPlanStatus.Submitted or KpiPlanStatus.Finalized)
            {
                plan.SubmittedAt = now.AddDays(-12);
                Step(plan, KpiAction.Submitted, KpiPlanStatus.Submitted, owner, null, plan.SubmittedAt.Value);
            }
            if (status == KpiPlanStatus.Finalized)
            {
                foreach (var item in plan.Items)
                {
                    var achievement = KpiCalculator.AchievementPercent(item.ActualValue ?? 0, item.Target);
                    item.AchievementPercent = Math.Round(achievement, 4, MidpointRounding.AwayFromZero);
                    item.WeightedScore = Math.Round(KpiCalculator.WeightedScore(achievement, item.Weight), 4, MidpointRounding.AwayFromZero);
                }
                plan.OverallScore = KpiCalculator.OverallScore(plan.Items.Select(i =>
                    KpiCalculator.WeightedScore(KpiCalculator.AchievementPercent(i.ActualValue ?? 0, i.Target), i.Weight)));
                plan.FinalizedAt = now.AddDays(-8);
                Step(plan, KpiAction.Finalized, KpiPlanStatus.Finalized, reviewer!, "Strong quarter.", plan.FinalizedAt.Value);
            }
            return plan;
        }

        void Step(KpiPlan plan, KpiAction action, KpiPlanStatus to, Employee actor, string? comment, DateTime at)
        {
            plan.StatusHistory.Add(new KpiPlanStatusHistory { Action = action, FromStatus = plan.Status, ToStatus = to, Actor = actor, Comment = comment, CreatedAt = at });
            plan.Status = to;
            plan.UpdatedAt = at;
        }

        var plans = new[]
        {
            // Active, partly filled in: Kunle updates actuals and submits
            Plan(s.Kunle, current, s.Ifeoma, KpiPlanStatus.Active,
            [
                ("Features delivered", "features", 12, 40, 7),
                ("Bugs resolved", "bugs", 50, 35, 31),
                ("Code reviews completed", "reviews", 40, 25, null)
            ]),
            // Submitted: Ifeoma finalizes or returns it
            Plan(s.Zainab, current, s.Ifeoma, KpiPlanStatus.Submitted,
            [
                ("Services migrated to Kubernetes", "services", 8, 50, 8),
                ("Incident response time", "% within SLA", 95, 30, 91),
                ("Mentoring sessions", "sessions", 12, 20, 10)
            ]),
            // Draft: HR activates it (reviewer defaults to Musa)
            Plan(s.Emeka, current, null, KpiPlanStatus.Draft,
            [
                ("Orders dispatched on time", "% on time", 98, 60, null),
                ("Stock count accuracy", "% accuracy", 99, 40, null)
            ]),
            // Finalized last quarter: target 4, actual 3, weight 20 -> 75% and 15.00 (the KPI-03 example)
            Plan(s.Kunle, previous, s.Ifeoma, KpiPlanStatus.Finalized,
            [
                ("Releases shipped", "releases", 4, 20, 3),
                ("Unit test coverage", "% coverage", 80, 50, 84),
                ("Documentation pages", "pages", 10, 30, 9)
            ]),
            // Active but past its deadline: edits return 409 until HR reopens it
            Plan(s.Zainab, previous, s.Ifeoma, KpiPlanStatus.Active,
            [
                ("Platform uptime", "% uptime", 99.9m, 70, 99.5m),
                ("Runbooks written", "runbooks", 5, 30, 2)
            ])
        };
        db.KpiPlans.AddRange(plans);

        Notify(db, s.Kunle, "New KPI plan assigned",
            $"Your KPI plan for {h2} is active. Record your results by {current.SubmissionDeadline:dd MMM yyyy}.", nameof(KpiPlan), now.AddDays(-57));
        Notify(db, s.Ifeoma, "KPI plan awaiting your review",
            $"Zainab Yusuf submitted their KPI plan for {h2}.", nameof(KpiPlan), now.AddDays(-12));

        await db.SaveChangesAsync();
    }

    // ------------------------------------------------------------------ Payroll

    private static async Task SeedPayrollAsync(AppDbContext db, Cast s)
    {
        var today = Clock.Today;
        var now = Clock.UtcNow;
        var current = new DateOnly(today.Year, today.Month, 1);
        var lastMonth = current.AddMonths(-1);
        var twoMonthsAgo = current.AddMonths(-2);

        if (await db.Payslips.AnyAsync(p => p.EmployeeId == s.Kunle.Id && p.Year == twoMonthsAgo.Year && p.Month == twoMonthsAgo.Month))
            return;

        var existing = await db.Payslips.Select(p => new { p.EmployeeId, p.Year, p.Month }).ToListAsync();
        bool Exists(Employee e, DateOnly period) => existing.Any(x => x.EmployeeId == e.Id && x.Year == period.Year && x.Month == period.Month);

        // Sample salary bands by grade level (NGN)
        static decimal Basic(Employee e) => e.Grade!.Level switch { 8 => 250_000m, 10 => 400_000m, 12 => 650_000m, _ => 900_000m };

        PayslipVersion Version(Employee e, int number, PayslipVersionStatus status, Employee preparer, Employee? publisher,
            DateTime createdAt, decimal extraAllowance = 0, string? reason = null)
        {
            var basic = Basic(e);
            var allowances = new List<(string, decimal)> { ("Housing", basic * 0.20m), ("Transport", 40_000m) };
            if (extraAllowance > 0) allowances.Add(("Overtime", extraAllowance));
            var gross = basic + allowances.Sum(a => a.Item2);
            var deductions = new List<(string, decimal)> { ("Pension (8%)", basic * 0.08m), ("PAYE", gross * 0.07m) };

            var (totalAllowances, totalDeductions, grossPay, netPay) =
                PayslipCalculator.Totals(basic, allowances.Select(a => a.Item2), deductions.Select(d => d.Item2));

            var version = new PayslipVersion
            {
                VersionNumber = number,
                Status = status,
                Currency = "NGN",
                BasicSalary = basic,
                TotalAllowances = totalAllowances,
                TotalDeductions = totalDeductions,
                GrossPay = grossPay,
                NetPay = netPay,
                RevisionReason = reason,
                PreparedBy = preparer,
                PublishedBy = publisher,
                PublishedAt = publisher == null ? null : createdAt.AddDays(1),
                CreatedAt = createdAt,
                UpdatedAt = createdAt
            };
            var order = 1;
            foreach (var (name, amount) in allowances)
                version.LineItems.Add(new PayslipLineItem { Type = PayslipLineType.Allowance, Name = name, Amount = PayslipCalculator.Round(amount), SortOrder = order++ });
            foreach (var (name, amount) in deductions)
                version.LineItems.Add(new PayslipLineItem { Type = PayslipLineType.Deduction, Name = name, Amount = PayslipCalculator.Round(amount), SortOrder = order++ });
            return version;
        }

        Payslip? Slip(Employee e, DateOnly period, params PayslipVersion[] versions)
        {
            if (Exists(e, period)) return null;
            var payslip = new Payslip { Employee = e, Month = period.Month, Year = period.Year, CreatedAt = versions[0].CreatedAt, UpdatedAt = now };
            foreach (var v in versions) payslip.Versions.Add(v);
            return payslip;
        }

        var monthEnd = (DateOnly d) => d.AddMonths(1).AddDays(-3).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var payslips = new List<Payslip?>();

        // Published for the last two months; Finance never handles their own (Amina's are done by Chinedu)
        foreach (var period in new[] { twoMonthsAgo, lastMonth })
        {
            foreach (var e in new[] { s.Kunle, s.Emeka, s.Ifeoma })
                payslips.Add(Slip(e, period, Version(e, 1, PayslipVersionStatus.Published, s.Amina, s.Amina, monthEnd(period))));
            payslips.Add(Slip(s.Amina, period, Version(s.Amina, 1, PayslipVersionStatus.Published, s.Chinedu, s.Chinedu, monthEnd(period))));
        }

        // Zainab: two months ago published; last month revised (v1 superseded, v2 published)
        payslips.Add(Slip(s.Zainab, twoMonthsAgo, Version(s.Zainab, 1, PayslipVersionStatus.Published, s.Amina, s.Amina, monthEnd(twoMonthsAgo))));
        var superseded = Version(s.Zainab, 1, PayslipVersionStatus.Superseded, s.Amina, s.Amina, monthEnd(lastMonth));
        superseded.SupersededAt = monthEnd(lastMonth).AddDays(4);
        payslips.Add(Slip(s.Zainab, lastMonth, superseded,
            Version(s.Zainab, 2, PayslipVersionStatus.Published, s.Amina, s.Chinedu, monthEnd(lastMonth).AddDays(3), 60_000m,
                "Overtime for the platform migration weekend was omitted.")));

        // This month: Kunle's is a draft (publish demo); Zainab's is still unpublished, so she can't see it
        payslips.Add(Slip(s.Kunle, current, Version(s.Kunle, 1, PayslipVersionStatus.Draft, s.Amina, null, now.AddDays(-1))));

        db.Payslips.AddRange(payslips.OfType<Payslip>());

        Notify(db, s.Zainab, "Your payslip was revised",
            $"Your payslip for {lastMonth:MMMM yyyy} was revised (version 2). Sign in to view the current version.", nameof(Payslip), now.AddDays(-5));

        await db.SaveChangesAsync();
    }

    // ------------------------------------------------------------------ Opportunities

    private static async Task SeedOpportunitiesAsync(AppDbContext db, Cast s)
    {
        const string marker = "Senior Software Engineer";
        if (await db.Opportunities.AnyAsync(o => o.Title == marker)) return;

        var now = Clock.UtcNow;

        Opportunity Opportunity(string title, OpportunityType type, Department department, OpportunityStatus status,
            DateTime closingAt, Department[] allowedDepartments, Grade[] allowedGrades, string eligibility)
        {
            var opportunity = new Opportunity
            {
                Title = title,
                Type = type,
                Department = department,
                Description = $"{title}: see the attached role profile for responsibilities and expectations.",
                Eligibility = eligibility,
                ClosingAt = closingAt,
                Status = status,
                CreatedBy = s.Bola,
                CreatedAt = now.AddDays(-20),
                UpdatedAt = now.AddDays(-20),
                PublishedAt = status == OpportunityStatus.Draft ? null : now.AddDays(-18),
                ClosedAt = status == OpportunityStatus.Closed ? closingAt : null
            };
            foreach (var d in allowedDepartments)
                opportunity.AllowedDepartments.Add(new OpportunityAllowedDepartment { Department = d });
            foreach (var g in allowedGrades)
                opportunity.AllowedGrades.Add(new OpportunityAllowedGrade { Grade = g });
            return opportunity;
        }

        OpportunityApplication Apply(Opportunity opportunity, Employee applicant, int daysAgo, params (ApplicationStatus To, Employee Actor, string? Comment, int DaysAgo)[] moves)
        {
            var submitted = now.AddDays(-daysAgo);
            var application = new OpportunityApplication
            {
                Opportunity = opportunity,
                Employee = applicant,
                SupportingStatement = $"I would like to be considered for {opportunity.Title}; my recent work in {applicant.Department!.Name} is directly relevant.",
                Status = ApplicationStatus.Submitted,
                DepartmentIdAtApply = applicant.DepartmentId,
                GradeIdAtApply = applicant.GradeId,
                EligibilityCheckedAt = submitted,
                SubmittedAt = submitted,
                UpdatedAt = submitted
            };
            application.StatusHistory.Add(new ApplicationStatusHistory { ToStatus = ApplicationStatus.Submitted, Actor = applicant, CreatedAt = submitted });
            foreach (var (to, actor, comment, ago) in moves)
            {
                application.StatusHistory.Add(new ApplicationStatusHistory
                    { FromStatus = application.Status, ToStatus = to, Actor = actor, Comment = comment, CreatedAt = now.AddDays(-ago) });
                application.Status = to;
                application.UpdatedAt = now.AddDays(-ago);
            }
            return application;
        }

        var engineering = s.Kunle.Department!;
        var operations = s.Emeka.Department!;
        var finance = s.Amina.Department!;
        var gl08 = s.Kunle.Grade!;
        var gl10 = s.Zainab.Grade!;

        // Open, restricted to Engineering GL-08/GL-10: Kunle is eligible, Emeka (Operations) gets 403
        var vacancy = Opportunity(marker, OpportunityType.InternalVacancy, engineering, OpportunityStatus.Published,
            now.AddDays(14), [engineering], [gl08, gl10], "Engineering staff on GL-08 or GL-10.");
        // Open to everyone
        var programme = Opportunity("Leadership Development Programme", OpportunityType.DevelopmentOpportunity, operations,
            OpportunityStatus.Published, now.AddDays(21), [], [], "Open to all staff.");
        // Draft: invisible to staff
        var draft = Opportunity("Finance Analyst", OpportunityType.InternalVacancy, finance, OpportunityStatus.Draft,
            now.AddDays(30), [finance], [], "Finance staff only.");
        // Closed
        var closed = Opportunity("Data Analytics Bootcamp", OpportunityType.DevelopmentOpportunity, engineering,
            OpportunityStatus.Closed, now.AddDays(-10), [], [], "Open to all staff.");

        db.Opportunities.AddRange(vacancy, programme, draft, closed);
        db.OpportunityApplications.AddRange(
            Apply(vacancy, s.Zainab, 6, (ApplicationStatus.Shortlisted, s.Bola, "Strong platform experience.", 2)),
            Apply(programme, s.Emeka, 4),
            Apply(closed, s.Kunle, 25,
                (ApplicationStatus.Shortlisted, s.Bola, null, 15),
                (ApplicationStatus.Selected, s.Bola, "Welcome to the cohort.", 9)));

        Notify(db, s.Zainab, "Application shortlisted",
            $"Your application for \"{marker}\" is now Shortlisted.", nameof(OpportunityApplication), now.AddDays(-2));

        await db.SaveChangesAsync();
    }

    // In-app only: seeded data never queues emails
    private static void Notify(AppDbContext db, Employee recipient, string title, string message, string relatedEntity, DateTime at) =>
        db.Notifications.Add(new Notification
        {
            Employee = recipient,
            Title = title,
            Message = message,
            RelatedEntity = relatedEntity,
            CreatedAt = at
        });
}
