using System;
using System.Collections.Generic;
using IMS.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace IMS.Api.Data;

public partial class AppDbContext : DbContext
{
    public AppDbContext()
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<academic_advisor_profile> academic_advisor_profiles { get; set; }

    public virtual DbSet<academic_evaluation> academic_evaluations { get; set; }

    public virtual DbSet<academic_evaluation_score> academic_evaluation_scores { get; set; }

    public virtual DbSet<administrator_profile> administrator_profiles { get; set; }

    public virtual DbSet<advisor_student_assignment> advisor_student_assignments { get; set; }

    public virtual DbSet<approval_action> approval_actions { get; set; }

    public virtual DbSet<approval_delegation_log> approval_delegation_logs { get; set; }

    public virtual DbSet<archived_record> archived_records { get; set; }

    public virtual DbSet<attendance_entry> attendance_entries { get; set; }

    public virtual DbSet<audit_log> audit_logs { get; set; }

    public virtual DbSet<backup_job> backup_jobs { get; set; }

    public virtual DbSet<company_evaluation> company_evaluations { get; set; }

    public virtual DbSet<company_evaluation_score> company_evaluation_scores { get; set; }

    public virtual DbSet<company_evaluation_template> company_evaluation_templates { get; set; }

    public virtual DbSet<company_evaluation_template_criterion> company_evaluation_template_criteria { get; set; }

    public virtual DbSet<email_outbox> email_outboxes { get; set; }

    public virtual DbSet<email_template> email_templates { get; set; }

    public virtual DbSet<field_visit> field_visits { get; set; }

    public virtual DbSet<final_evaluation_request> final_evaluation_requests { get; set; }

    public virtual DbSet<helper_opportunity> helper_opportunities { get; set; }

    public virtual DbSet<helper_opportunity_requirement> helper_opportunity_requirements { get; set; }

    public virtual DbSet<helper_opportunity_task> helper_opportunity_tasks { get; set; }

    public virtual DbSet<helper_provider> helper_providers { get; set; }

    public virtual DbSet<internship> internships { get; set; }

    public virtual DbSet<invitation_batch> invitation_batches { get; set; }

    public virtual DbSet<invitation_recipient> invitation_recipients { get; set; }

    public virtual DbSet<notification> notifications { get; set; }

    public virtual DbSet<online_follow_up> online_follow_ups { get; set; }

    public virtual DbSet<permission> permissions { get; set; }

    public virtual DbSet<role> roles { get; set; }

    public virtual DbSet<role_permission> role_permissions { get; set; }

    public virtual DbSet<student_course> student_courses { get; set; }

    public virtual DbSet<student_document> student_documents { get; set; }

    public virtual DbSet<student_eligibility_review> student_eligibility_reviews { get; set; }

    public virtual DbSet<student_profile> student_profiles { get; set; }

    public virtual DbSet<student_project> student_projects { get; set; }

    public virtual DbSet<student_skill> student_skills { get; set; }

    public virtual DbSet<system_configuration> system_configurations { get; set; }

    public virtual DbSet<training_company_request> training_company_requests { get; set; }

    public virtual DbSet<training_plan> training_plans { get; set; }

    public virtual DbSet<training_task> training_tasks { get; set; }

    public virtual DbSet<training_task_evidence> training_task_evidences { get; set; }

    public virtual DbSet<user> users { get; set; }

    public virtual DbSet<user_role> user_roles { get; set; }

    public virtual DbSet<user_session> user_sessions { get; set; }

    public virtual DbSet<vw_attendance_summary> vw_attendance_summaries { get; set; }

    public virtual DbSet<vw_student_current_advisor> vw_student_current_advisors { get; set; }

    public virtual DbSet<weekly_report> weekly_reports { get; set; }

    public virtual DbSet<weekly_report_item> weekly_report_items { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasPostgresEnum("approval_status_enum", new[] { "Draft", "Pending", "Approved", "Rejected", "ChangesRequested" })
            .HasPostgresEnum("approver_type_enum", new[] { "AcademicAdvisor", "Administrator" })
            .HasPostgresEnum("assignment_status_enum", new[] { "Active", "Inactive" })
            .HasPostgresEnum("attendance_status_enum", new[] { "Present", "Absent" })
            .HasPostgresEnum("backup_job_status_enum", new[] { "Pending", "Running", "Success", "Failed" })
            .HasPostgresEnum("document_category_enum", new[] { "CV", "Portfolio", "Certificate", "OtherAttachment", "TrainingLetter" })
            .HasPostgresEnum("document_status_enum", new[] { "Uploaded", "Reviewed", "Archived", "Generated" })
            .HasPostgresEnum("evaluation_status_enum", new[] { "Pending", "Submitted", "Approved", "Rejected" })
            .HasPostgresEnum("evaluation_template_status_enum", new[] { "Draft", "Active", "Inactive" })
            .HasPostgresEnum("file_type_enum", new[] { "PDF", "DOCX", "PPTX", "PNG", "JPG", "JPEG", "OTHER" })
            .HasPostgresEnum("helper_opportunity_status_enum", new[] { "Open", "Closed", "Archived" })
            .HasPostgresEnum("invitation_mode_enum", new[] { "Excel", "Link" })
            .HasPostgresEnum("invitation_status_enum", new[] { "Draft", "Sent", "Accepted", "Expired", "Cancelled" })
            .HasPostgresEnum("notification_channel_enum", new[] { "InApp", "Email" })
            .HasPostgresEnum("notification_status_enum", new[] { "Pending", "Sent", "Read", "Failed" })
            .HasPostgresEnum("notification_type_enum", new[] { "System", "Email", "Reminder", "Alert" })
            .HasPostgresEnum("request_type_enum", new[] { "StudentEligibility", "TrainingCompanyRequest", "TrainingPlan", "WeeklyReport", "FinalEvaluationRequest" })
            .HasPostgresEnum("skill_level_enum", new[] { "Beginner", "Intermediate", "Advanced" })
            .HasPostgresEnum("user_status_enum", new[] { "Pending", "Active", "Inactive" });

        modelBuilder.Entity<academic_advisor_profile>(entity =>
        {
            entity.HasKey(e => e.user_id).HasName("academic_advisor_profiles_pkey");

            entity.Property(e => e.user_id).ValueGeneratedNever();
            entity.Property(e => e.created_at).HasDefaultValueSql("now()");
            entity.Property(e => e.department).HasMaxLength(150);
            entity.Property(e => e.employee_no).HasMaxLength(100);
            entity.Property(e => e.is_system_responsible).HasDefaultValue(false);
            entity.Property(e => e.updated_at).HasDefaultValueSql("now()");

            entity.HasOne(d => d.user).WithOne(p => p.academic_advisor_profile)
                .HasForeignKey<academic_advisor_profile>(d => d.user_id)
                .HasConstraintName("academic_advisor_profiles_user_id_fkey");
        });

        modelBuilder.Entity<academic_evaluation>(entity =>
        {
            entity.HasKey(e => e.id).HasName("academic_evaluations_pkey");

            entity.HasIndex(e => e.internship_id, "academic_evaluations_internship_id_key").IsUnique();

            entity.Property(e => e.evaluator_name).HasMaxLength(200);
            entity.Property(e => e.submitted_at).HasDefaultValueSql("now()");
            entity.Property(e => e.total_percentage).HasPrecision(6, 2);

            entity.HasOne(d => d.evaluator_user).WithMany(p => p.academic_evaluationevaluator_users)
                .HasForeignKey(d => d.evaluator_user_id)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("academic_evaluations_evaluator_user_id_fkey");

            entity.HasOne(d => d.internship).WithOne(p => p.academic_evaluation)
                .HasForeignKey<academic_evaluation>(d => d.internship_id)
                .HasConstraintName("academic_evaluations_internship_id_fkey");

            entity.HasOne(d => d.student_user).WithMany(p => p.academic_evaluationstudent_users)
                .HasForeignKey(d => d.student_user_id)
                .HasConstraintName("academic_evaluations_student_user_id_fkey");
        });

        modelBuilder.Entity<academic_evaluation_score>(entity =>
        {
            entity.HasKey(e => e.id).HasName("academic_evaluation_scores_pkey");

            entity.Property(e => e.criterion_name).HasMaxLength(250);
            entity.Property(e => e.out_of).HasPrecision(6, 2);
            entity.Property(e => e.score).HasPrecision(6, 2);

            entity.HasOne(d => d.academic_evaluation).WithMany(p => p.academic_evaluation_scores)
                .HasForeignKey(d => d.academic_evaluation_id)
                .HasConstraintName("academic_evaluation_scores_academic_evaluation_id_fkey");
        });

        modelBuilder.Entity<administrator_profile>(entity =>
        {
            entity.HasKey(e => e.user_id).HasName("administrator_profiles_pkey");

            entity.Property(e => e.user_id).ValueGeneratedNever();
            entity.Property(e => e.created_at).HasDefaultValueSql("now()");
            entity.Property(e => e.department).HasMaxLength(150);
            entity.Property(e => e.employee_no).HasMaxLength(100);
            entity.Property(e => e.updated_at).HasDefaultValueSql("now()");

            entity.HasOne(d => d.user).WithOne(p => p.administrator_profile)
                .HasForeignKey<administrator_profile>(d => d.user_id)
                .HasConstraintName("administrator_profiles_user_id_fkey");
        });

        modelBuilder.Entity<advisor_student_assignment>(entity =>
        {
            entity.HasKey(e => e.id).HasName("advisor_student_assignments_pkey");

            entity.HasIndex(e => e.student_user_id, "ux_student_one_active_advisor")
                .IsUnique()
                .HasFilter("(status = 'Active'::assignment_status_enum)");

            entity.Property(e => e.assignment_start_at).HasDefaultValueSql("now()");
            entity.Property(e => e.created_at).HasDefaultValueSql("now()");
            entity.Property(e => e.updated_at).HasDefaultValueSql("now()");

            entity.HasOne(d => d.advisor_user).WithMany(p => p.advisor_student_assignmentadvisor_users)
                .HasForeignKey(d => d.advisor_user_id)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("advisor_student_assignments_advisor_user_id_fkey");

            entity.HasOne(d => d.assigned_by_user).WithMany(p => p.advisor_student_assignmentassigned_by_users)
                .HasForeignKey(d => d.assigned_by_user_id)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("advisor_student_assignments_assigned_by_user_id_fkey");

            entity.HasOne(d => d.student_user).WithOne(p => p.advisor_student_assignmentstudent_user)
                .HasForeignKey<advisor_student_assignment>(d => d.student_user_id)
                .HasConstraintName("advisor_student_assignments_student_user_id_fkey");
        });

        modelBuilder.Entity<approval_action>(entity =>
        {
            entity.HasKey(e => e.id).HasName("approval_actions_pkey");

            entity.Property(e => e.acted_at).HasDefaultValueSql("now()");

            entity.HasOne(d => d.actor_user).WithMany(p => p.approval_actionactor_users)
                .HasForeignKey(d => d.actor_user_id)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("approval_actions_actor_user_id_fkey");

            entity.HasOne(d => d.student_user).WithMany(p => p.approval_actionstudent_users)
                .HasForeignKey(d => d.student_user_id)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("approval_actions_student_user_id_fkey");
        });

        modelBuilder.Entity<approval_delegation_log>(entity =>
        {
            entity.HasKey(e => e.id).HasName("approval_delegation_logs_pkey");

            entity.Property(e => e.changed_at).HasDefaultValueSql("now()");

            entity.HasOne(d => d.changed_by_user).WithMany(p => p.approval_delegation_logchanged_by_users)
                .HasForeignKey(d => d.changed_by_user_id)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("approval_delegation_logs_changed_by_user_id_fkey");

            entity.HasOne(d => d.from_owner_user).WithMany(p => p.approval_delegation_logfrom_owner_users)
                .HasForeignKey(d => d.from_owner_user_id)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("approval_delegation_logs_from_owner_user_id_fkey");

            entity.HasOne(d => d.student_user).WithMany(p => p.approval_delegation_logstudent_users)
                .HasForeignKey(d => d.student_user_id)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("approval_delegation_logs_student_user_id_fkey");

            entity.HasOne(d => d.to_owner_user).WithMany(p => p.approval_delegation_logto_owner_users)
                .HasForeignKey(d => d.to_owner_user_id)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("approval_delegation_logs_to_owner_user_id_fkey");
        });

        modelBuilder.Entity<archived_record>(entity =>
        {
            entity.HasKey(e => e.id).HasName("archived_records_pkey");

            entity.Property(e => e.archived_at).HasDefaultValueSql("now()");
            entity.Property(e => e.entity_name).HasMaxLength(150);
            entity.Property(e => e.payload).HasColumnType("jsonb");
            entity.Property(e => e.record_reference).HasMaxLength(150);

            entity.HasOne(d => d.archived_by_user).WithMany(p => p.archived_records)
                .HasForeignKey(d => d.archived_by_user_id)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("archived_records_archived_by_user_id_fkey");
        });

        modelBuilder.Entity<attendance_entry>(entity =>
        {
            entity.HasKey(e => e.id).HasName("attendance_entries_pkey");

            entity.HasIndex(e => new { e.internship_id, e.entry_date }, "attendance_entries_internship_id_entry_date_key").IsUnique();

            entity.Property(e => e.created_at).HasDefaultValueSql("now()");
            entity.Property(e => e.daily_hours).HasPrecision(5, 2);
            entity.Property(e => e.updated_at).HasDefaultValueSql("now()");

            entity.HasOne(d => d.created_by_user).WithMany(p => p.attendance_entrycreated_by_users)
                .HasForeignKey(d => d.created_by_user_id)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("attendance_entries_created_by_user_id_fkey");

            entity.HasOne(d => d.internship).WithMany(p => p.attendance_entries)
                .HasForeignKey(d => d.internship_id)
                .HasConstraintName("attendance_entries_internship_id_fkey");

            entity.HasOne(d => d.student_user).WithMany(p => p.attendance_entrystudent_users)
                .HasForeignKey(d => d.student_user_id)
                .HasConstraintName("attendance_entries_student_user_id_fkey");
        });

        modelBuilder.Entity<audit_log>(entity =>
        {
            entity.HasKey(e => e.id).HasName("audit_logs_pkey");

            entity.Property(e => e.action).HasMaxLength(200);
            entity.Property(e => e.created_at).HasDefaultValueSql("now()");
            entity.Property(e => e.entity_id).HasMaxLength(100);
            entity.Property(e => e.entity_name).HasMaxLength(150);
            entity.Property(e => e.metadata).HasColumnType("jsonb");

            entity.HasOne(d => d.actor_user).WithMany(p => p.audit_logs)
                .HasForeignKey(d => d.actor_user_id)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("audit_logs_actor_user_id_fkey");
        });

        modelBuilder.Entity<backup_job>(entity =>
        {
            entity.HasKey(e => e.id).HasName("backup_jobs_pkey");

            entity.Property(e => e.created_at).HasDefaultValueSql("now()");
            entity.Property(e => e.job_name).HasMaxLength(200);
            entity.Property(e => e.schedule).HasMaxLength(150);
        });

        modelBuilder.Entity<company_evaluation>(entity =>
        {
            entity.HasKey(e => e.id).HasName("company_evaluations_pkey");

            entity.Property(e => e.evaluator_job_title).HasMaxLength(200);
            entity.Property(e => e.evaluator_name).HasMaxLength(200);
            entity.Property(e => e.provider_email).HasMaxLength(255);
            entity.Property(e => e.provider_name).HasMaxLength(250);
            entity.Property(e => e.submitted_at).HasDefaultValueSql("now()");
            entity.Property(e => e.total_percentage).HasPrecision(6, 2);

            entity.HasOne(d => d.internship).WithMany(p => p.company_evaluations)
                .HasForeignKey(d => d.internship_id)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("company_evaluations_internship_id_fkey");

            entity.HasOne(d => d.student_user).WithMany(p => p.company_evaluations)
                .HasForeignKey(d => d.student_user_id)
                .HasConstraintName("company_evaluations_student_user_id_fkey");

            entity.HasOne(d => d.template).WithMany(p => p.company_evaluations)
                .HasForeignKey(d => d.template_id)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("company_evaluations_template_id_fkey");
        });

        modelBuilder.Entity<company_evaluation_score>(entity =>
        {
            entity.HasKey(e => e.id).HasName("company_evaluation_scores_pkey");

            entity.Property(e => e.criterion_name).HasMaxLength(250);
            entity.Property(e => e.out_of).HasPrecision(6, 2);
            entity.Property(e => e.score).HasPrecision(6, 2);

            entity.HasOne(d => d.company_evaluation).WithMany(p => p.company_evaluation_scores)
                .HasForeignKey(d => d.company_evaluation_id)
                .HasConstraintName("company_evaluation_scores_company_evaluation_id_fkey");

            entity.HasOne(d => d.criterion).WithMany(p => p.company_evaluation_scores)
                .HasForeignKey(d => d.criterion_id)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("company_evaluation_scores_criterion_id_fkey");
        });

        modelBuilder.Entity<company_evaluation_template>(entity =>
        {
            entity.HasKey(e => e.id).HasName("company_evaluation_templates_pkey");

            entity.HasIndex(e => e.token, "company_evaluation_templates_token_key").IsUnique();

            entity.Property(e => e.created_at).HasDefaultValueSql("now()");
            entity.Property(e => e.provider_email).HasMaxLength(255);
            entity.Property(e => e.provider_name).HasMaxLength(250);
            entity.Property(e => e.title)
                .HasMaxLength(250)
                .HasDefaultValueSql("'Company Evaluation Form'::character varying");
            entity.Property(e => e.token).HasMaxLength(250);
            entity.Property(e => e.updated_at).HasDefaultValueSql("now()");
            entity.Property(e => e.version)
                .HasMaxLength(50)
                .HasDefaultValueSql("'v1.0'::character varying");

            entity.HasOne(d => d.created_by_user).WithMany(p => p.company_evaluation_templatecreated_by_users)
                .HasForeignKey(d => d.created_by_user_id)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("company_evaluation_templates_created_by_user_id_fkey");

            entity.HasOne(d => d.internship).WithMany(p => p.company_evaluation_templates)
                .HasForeignKey(d => d.internship_id)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("company_evaluation_templates_internship_id_fkey");

            entity.HasOne(d => d.student_user).WithMany(p => p.company_evaluation_templatestudent_users)
                .HasForeignKey(d => d.student_user_id)
                .HasConstraintName("company_evaluation_templates_student_user_id_fkey");
        });

        modelBuilder.Entity<company_evaluation_template_criterion>(entity =>
        {
            entity.HasKey(e => e.id).HasName("company_evaluation_template_criteria_pkey");

            entity.Property(e => e.criterion_name).HasMaxLength(250);
            entity.Property(e => e.sort_order).HasDefaultValue(1);
            entity.Property(e => e.weight).HasPrecision(6, 2);

            entity.HasOne(d => d.template).WithMany(p => p.company_evaluation_template_criteria)
                .HasForeignKey(d => d.template_id)
                .HasConstraintName("company_evaluation_template_criteria_template_id_fkey");
        });

        modelBuilder.Entity<email_outbox>(entity =>
        {
            entity.HasKey(e => e.id).HasName("email_outbox_pkey");

            entity.ToTable("email_outbox");

            entity.Property(e => e.queued_at).HasDefaultValueSql("now()");
            entity.Property(e => e.related_entity_type).HasMaxLength(150);
            entity.Property(e => e.to_email).HasMaxLength(255);

            entity.HasOne(d => d.template).WithMany(p => p.email_outboxes)
                .HasForeignKey(d => d.template_id)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("email_outbox_template_id_fkey");
        });

        modelBuilder.Entity<email_template>(entity =>
        {
            entity.HasKey(e => e.id).HasName("email_templates_pkey");

            entity.HasIndex(e => e.name, "email_templates_name_key").IsUnique();

            entity.Property(e => e.category).HasMaxLength(150);
            entity.Property(e => e.created_at).HasDefaultValueSql("now()");
            entity.Property(e => e.is_active).HasDefaultValue(true);
            entity.Property(e => e.name).HasMaxLength(200);
            entity.Property(e => e.updated_at).HasDefaultValueSql("now()");
        });

        modelBuilder.Entity<field_visit>(entity =>
        {
            entity.HasKey(e => e.id).HasName("field_visits_pkey");

            entity.Property(e => e.created_at).HasDefaultValueSql("now()");
            entity.Property(e => e.location).HasMaxLength(250);
            entity.Property(e => e.status)
                .HasMaxLength(100)
                .HasDefaultValueSql("'Scheduled'::character varying");

            entity.HasOne(d => d.advisor_user).WithMany(p => p.field_visits)
                .HasForeignKey(d => d.advisor_user_id)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("field_visits_advisor_user_id_fkey");

            entity.HasOne(d => d.internship).WithMany(p => p.field_visits)
                .HasForeignKey(d => d.internship_id)
                .HasConstraintName("field_visits_internship_id_fkey");
        });

        modelBuilder.Entity<final_evaluation_request>(entity =>
        {
            entity.HasKey(e => e.id).HasName("final_evaluation_requests_pkey");

            entity.Property(e => e.created_at).HasDefaultValueSql("now()");
            entity.Property(e => e.evaluation_template_name).HasMaxLength(250);
            entity.Property(e => e.provider_email).HasMaxLength(255);
            entity.Property(e => e.provider_name).HasMaxLength(250);
            entity.Property(e => e.requested_at).HasDefaultValueSql("now()");
            entity.Property(e => e.sending_template_name).HasMaxLength(250);

            entity.HasOne(d => d.company_evaluation_template).WithMany(p => p.final_evaluation_requests)
                .HasForeignKey(d => d.company_evaluation_template_id)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("final_evaluation_requests_company_evaluation_template_id_fkey");

            entity.HasOne(d => d.internship).WithMany(p => p.final_evaluation_requests)
                .HasForeignKey(d => d.internship_id)
                .HasConstraintName("final_evaluation_requests_internship_id_fkey");

            entity.HasOne(d => d.requested_by_user).WithMany(p => p.final_evaluation_requestrequested_by_users)
                .HasForeignKey(d => d.requested_by_user_id)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("final_evaluation_requests_requested_by_user_id_fkey");

            entity.HasOne(d => d.student_user).WithMany(p => p.final_evaluation_requeststudent_users)
                .HasForeignKey(d => d.student_user_id)
                .HasConstraintName("final_evaluation_requests_student_user_id_fkey");
        });

        modelBuilder.Entity<helper_opportunity>(entity =>
        {
            entity.HasKey(e => e.id).HasName("helper_opportunities_pkey");

            entity.Property(e => e.created_at).HasDefaultValueSql("now()");
            entity.Property(e => e.location).HasMaxLength(150);
            entity.Property(e => e.source).HasMaxLength(150);
            entity.Property(e => e.title).HasMaxLength(250);
            entity.Property(e => e.updated_at).HasDefaultValueSql("now()");
            entity.Property(e => e.work_mode).HasMaxLength(100);

            entity.HasOne(d => d.provider).WithMany(p => p.helper_opportunities)
                .HasForeignKey(d => d.provider_id)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("helper_opportunities_provider_id_fkey");
        });

        modelBuilder.Entity<helper_opportunity_requirement>(entity =>
        {
            entity.HasKey(e => e.id).HasName("helper_opportunity_requirements_pkey");

            entity.Property(e => e.sort_order).HasDefaultValue(1);

            entity.HasOne(d => d.opportunity).WithMany(p => p.helper_opportunity_requirements)
                .HasForeignKey(d => d.opportunity_id)
                .HasConstraintName("helper_opportunity_requirements_opportunity_id_fkey");
        });

        modelBuilder.Entity<helper_opportunity_task>(entity =>
        {
            entity.HasKey(e => e.id).HasName("helper_opportunity_tasks_pkey");

            entity.Property(e => e.sort_order).HasDefaultValue(1);
            entity.Property(e => e.task_title).HasMaxLength(250);

            entity.HasOne(d => d.opportunity).WithMany(p => p.helper_opportunity_tasks)
                .HasForeignKey(d => d.opportunity_id)
                .HasConstraintName("helper_opportunity_tasks_opportunity_id_fkey");
        });

        modelBuilder.Entity<helper_provider>(entity =>
        {
            entity.HasKey(e => e.id).HasName("helper_providers_pkey");

            entity.Property(e => e.city).HasMaxLength(150);
            entity.Property(e => e.created_at).HasDefaultValueSql("now()");
            entity.Property(e => e.email).HasMaxLength(255);
            entity.Property(e => e.name).HasMaxLength(250);
            entity.Property(e => e.phone).HasMaxLength(50);
            entity.Property(e => e.sector).HasMaxLength(150);
            entity.Property(e => e.updated_at).HasDefaultValueSql("now()");
        });

        modelBuilder.Entity<internship>(entity =>
        {
            entity.HasKey(e => e.id).HasName("internships_pkey");

            entity.HasIndex(e => e.company_request_id, "internships_company_request_id_key").IsUnique();

            entity.HasIndex(e => e.student_user_id, "ux_internships_student_active")
                .IsUnique()
                .HasFilter("((status)::text = ANY ((ARRAY['Approved'::character varying, 'InProgress'::character varying])::text[]))");

            entity.Property(e => e.approved_by_academic_advisor).HasDefaultValue(true);
            entity.Property(e => e.created_at).HasDefaultValueSql("now()");
            entity.Property(e => e.internship_title).HasMaxLength(250);
            entity.Property(e => e.provider_email).HasMaxLength(255);
            entity.Property(e => e.provider_name).HasMaxLength(250);
            entity.Property(e => e.status)
                .HasMaxLength(100)
                .HasDefaultValueSql("'Approved'::character varying");
            entity.Property(e => e.updated_at).HasDefaultValueSql("now()");

            entity.HasOne(d => d.company_request).WithOne(p => p.internship)
                .HasForeignKey<internship>(d => d.company_request_id)
                .HasConstraintName("internships_company_request_id_fkey");

            entity.HasOne(d => d.student_user).WithOne(p => p.internship)
                .HasForeignKey<internship>(d => d.student_user_id)
                .HasConstraintName("internships_student_user_id_fkey");
        });

        modelBuilder.Entity<invitation_batch>(entity =>
        {
            entity.HasKey(e => e.id).HasName("invitation_batches_pkey");

            entity.HasIndex(e => e.shared_link_token, "ux_invitation_batches_shared_link_token")
                .IsUnique()
                .HasFilter("(shared_link_token IS NOT NULL)");

            entity.Property(e => e.created_at).HasDefaultValueSql("now()");
            entity.Property(e => e.excel_file_name).HasMaxLength(255);
            entity.Property(e => e.shared_link_token).HasMaxLength(200);
            entity.Property(e => e.total_recipients).HasDefaultValue(0);

            entity.HasOne(d => d.advisor_user).WithMany(p => p.invitation_batchadvisor_users)
                .HasForeignKey(d => d.advisor_user_id)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("invitation_batches_advisor_user_id_fkey");

            entity.HasOne(d => d.created_by_user).WithMany(p => p.invitation_batchcreated_by_users)
                .HasForeignKey(d => d.created_by_user_id)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("invitation_batches_created_by_user_id_fkey");
        });

        modelBuilder.Entity<invitation_recipient>(entity =>
        {
            entity.HasKey(e => e.id).HasName("invitation_recipients_pkey");

            entity.Property(e => e.created_at).HasDefaultValueSql("now()");
            entity.Property(e => e.student_email).HasMaxLength(255);
            entity.Property(e => e.student_name).HasMaxLength(200);

            entity.HasOne(d => d.advisor_user).WithMany(p => p.invitation_recipientadvisor_users)
                .HasForeignKey(d => d.advisor_user_id)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("invitation_recipients_advisor_user_id_fkey");

            entity.HasOne(d => d.batch).WithMany(p => p.invitation_recipients)
                .HasForeignKey(d => d.batch_id)
                .HasConstraintName("invitation_recipients_batch_id_fkey");

            entity.HasOne(d => d.student_user).WithMany(p => p.invitation_recipientstudent_users)
                .HasForeignKey(d => d.student_user_id)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("invitation_recipients_student_user_id_fkey");
        });

        modelBuilder.Entity<notification>(entity =>
        {
            entity.HasKey(e => e.id).HasName("notifications_pkey");

            entity.Property(e => e.created_at).HasDefaultValueSql("now()");
            entity.Property(e => e.recipient_email).HasMaxLength(255);
            entity.Property(e => e.related_entity_type).HasMaxLength(150);
            entity.Property(e => e.title).HasMaxLength(250);

            entity.HasOne(d => d.user).WithMany(p => p.notifications)
                .HasForeignKey(d => d.user_id)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("notifications_user_id_fkey");
        });

        modelBuilder.Entity<online_follow_up>(entity =>
        {
            entity.HasKey(e => e.id).HasName("online_follow_ups_pkey");

            entity.Property(e => e.created_at).HasDefaultValueSql("now()");
            entity.Property(e => e.status)
                .HasMaxLength(100)
                .HasDefaultValueSql("'Scheduled'::character varying");

            entity.HasOne(d => d.advisor_user).WithMany(p => p.online_follow_ups)
                .HasForeignKey(d => d.advisor_user_id)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("online_follow_ups_advisor_user_id_fkey");

            entity.HasOne(d => d.internship).WithMany(p => p.online_follow_ups)
                .HasForeignKey(d => d.internship_id)
                .HasConstraintName("online_follow_ups_internship_id_fkey");
        });

        modelBuilder.Entity<permission>(entity =>
        {
            entity.HasKey(e => e.id).HasName("permissions_pkey");

            entity.HasIndex(e => e.code, "permissions_code_key").IsUnique();

            entity.Property(e => e.code).HasMaxLength(150);
            entity.Property(e => e.created_at).HasDefaultValueSql("now()");
            entity.Property(e => e.name).HasMaxLength(200);
        });

        modelBuilder.Entity<role>(entity =>
        {
            entity.HasKey(e => e.id).HasName("roles_pkey");

            entity.HasIndex(e => e.code, "roles_code_key").IsUnique();

            entity.HasIndex(e => e.name, "roles_name_key").IsUnique();

            entity.Property(e => e.code).HasMaxLength(100);
            entity.Property(e => e.created_at).HasDefaultValueSql("now()");
            entity.Property(e => e.name).HasMaxLength(150);
        });

        modelBuilder.Entity<role_permission>(entity =>
        {
            entity.HasKey(e => e.id).HasName("role_permissions_pkey");

            entity.HasIndex(e => new { e.role_id, e.permission_id }, "role_permissions_role_id_permission_id_key").IsUnique();

            entity.Property(e => e.created_at).HasDefaultValueSql("now()");

            entity.HasOne(d => d.permission).WithMany(p => p.role_permissions)
                .HasForeignKey(d => d.permission_id)
                .HasConstraintName("role_permissions_permission_id_fkey");

            entity.HasOne(d => d.role).WithMany(p => p.role_permissions)
                .HasForeignKey(d => d.role_id)
                .HasConstraintName("role_permissions_role_id_fkey");
        });

        modelBuilder.Entity<student_course>(entity =>
        {
            entity.HasKey(e => e.id).HasName("student_courses_pkey");

            entity.Property(e => e.created_at).HasDefaultValueSql("now()");
            entity.Property(e => e.provider).HasMaxLength(200);
            entity.Property(e => e.title).HasMaxLength(250);

            entity.HasOne(d => d.student_user).WithMany(p => p.student_courses)
                .HasForeignKey(d => d.student_user_id)
                .HasConstraintName("student_courses_student_user_id_fkey");
        });

        modelBuilder.Entity<student_document>(entity =>
        {
            entity.HasKey(e => e.id).HasName("student_documents_pkey");

            entity.Property(e => e.file_name).HasMaxLength(255);
            entity.Property(e => e.title).HasMaxLength(250);
            entity.Property(e => e.uploaded_at).HasDefaultValueSql("now()");

            entity.HasOne(d => d.generated_by_user).WithMany(p => p.student_documentgenerated_by_users)
                .HasForeignKey(d => d.generated_by_user_id)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("student_documents_generated_by_user_id_fkey");

            entity.HasOne(d => d.student_user).WithMany(p => p.student_documentstudent_users)
                .HasForeignKey(d => d.student_user_id)
                .HasConstraintName("student_documents_student_user_id_fkey");
        });

        modelBuilder.Entity<student_eligibility_review>(entity =>
        {
            entity.HasKey(e => e.id).HasName("student_eligibility_reviews_pkey");

            entity.HasIndex(e => e.student_user_id, "ux_student_eligibility_current").IsUnique();

            entity.Property(e => e.created_at).HasDefaultValueSql("now()");
            entity.Property(e => e.updated_at).HasDefaultValueSql("now()");

            entity.HasOne(d => d.advisor_assignment).WithMany(p => p.student_eligibility_reviews)
                .HasForeignKey(d => d.advisor_assignment_id)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("student_eligibility_reviews_advisor_assignment_id_fkey");

            entity.HasOne(d => d.approval_owner_user).WithMany(p => p.student_eligibility_reviewapproval_owner_users)
                .HasForeignKey(d => d.approval_owner_user_id)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("student_eligibility_reviews_approval_owner_user_id_fkey");

            entity.HasOne(d => d.invitation_recipient).WithMany(p => p.student_eligibility_reviews)
                .HasForeignKey(d => d.invitation_recipient_id)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("student_eligibility_reviews_invitation_recipient_id_fkey");

            entity.HasOne(d => d.reviewer_user).WithMany(p => p.student_eligibility_reviewreviewer_users)
                .HasForeignKey(d => d.reviewer_user_id)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("student_eligibility_reviews_reviewer_user_id_fkey");

            entity.HasOne(d => d.student_user).WithOne(p => p.student_eligibility_reviewstudent_user)
                .HasForeignKey<student_eligibility_review>(d => d.student_user_id)
                .HasConstraintName("student_eligibility_reviews_student_user_id_fkey");
        });

        modelBuilder.Entity<student_profile>(entity =>
        {
            entity.HasKey(e => e.user_id).HasName("student_profiles_pkey");

            entity.Property(e => e.user_id).ValueGeneratedNever();
            entity.Property(e => e.city).HasMaxLength(150);
            entity.Property(e => e.country).HasMaxLength(150);
            entity.Property(e => e.created_at).HasDefaultValueSql("now()");
            entity.Property(e => e.gpa).HasPrecision(4, 2);
            entity.Property(e => e.headline).HasMaxLength(250);
            entity.Property(e => e.major).HasMaxLength(200);
            entity.Property(e => e.student_code).HasMaxLength(100);
            entity.Property(e => e.university).HasMaxLength(200);
            entity.Property(e => e.updated_at).HasDefaultValueSql("now()");

            entity.HasOne(d => d.user).WithOne(p => p.student_profile)
                .HasForeignKey<student_profile>(d => d.user_id)
                .HasConstraintName("student_profiles_user_id_fkey");
        });

        modelBuilder.Entity<student_project>(entity =>
        {
            entity.HasKey(e => e.id).HasName("student_projects_pkey");

            entity.Property(e => e.created_at).HasDefaultValueSql("now()");
            entity.Property(e => e.role_name).HasMaxLength(150);
            entity.Property(e => e.title).HasMaxLength(250);

            entity.HasOne(d => d.student_user).WithMany(p => p.student_projects)
                .HasForeignKey(d => d.student_user_id)
                .HasConstraintName("student_projects_student_user_id_fkey");
        });

        modelBuilder.Entity<student_skill>(entity =>
        {
            entity.HasKey(e => e.id).HasName("student_skills_pkey");

            entity.Property(e => e.category).HasMaxLength(150);
            entity.Property(e => e.created_at).HasDefaultValueSql("now()");
            entity.Property(e => e.name).HasMaxLength(200);

            entity.HasOne(d => d.student_user).WithMany(p => p.student_skills)
                .HasForeignKey(d => d.student_user_id)
                .HasConstraintName("student_skills_student_user_id_fkey");
        });

        modelBuilder.Entity<system_configuration>(entity =>
        {
            entity.HasKey(e => e.id).HasName("system_configurations_pkey");

            entity.HasIndex(e => e.key, "system_configurations_key_key").IsUnique();

            entity.Property(e => e.category).HasMaxLength(150);
            entity.Property(e => e.is_editable).HasDefaultValue(true);
            entity.Property(e => e.key).HasMaxLength(200);
            entity.Property(e => e.updated_at).HasDefaultValueSql("now()");
        });

        modelBuilder.Entity<training_company_request>(entity =>
        {
            entity.HasKey(e => e.id).HasName("training_company_requests_pkey");

            entity.Property(e => e.city).HasMaxLength(150);
            entity.Property(e => e.contact_name).HasMaxLength(200);
            entity.Property(e => e.contact_phone).HasMaxLength(50);
            entity.Property(e => e.created_at).HasDefaultValueSql("now()");
            entity.Property(e => e.opportunity_title).HasMaxLength(250);
            entity.Property(e => e.provider_email).HasMaxLength(255);
            entity.Property(e => e.provider_name).HasMaxLength(250);
            entity.Property(e => e.sector).HasMaxLength(150);
            entity.Property(e => e.submitted_at).HasDefaultValueSql("now()");
            entity.Property(e => e.updated_at).HasDefaultValueSql("now()");

            entity.HasOne(d => d.advisor_assignment).WithMany(p => p.training_company_requests)
                .HasForeignKey(d => d.advisor_assignment_id)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("training_company_requests_advisor_assignment_id_fkey");

            entity.HasOne(d => d.approval_owner_user).WithMany(p => p.training_company_requestapproval_owner_users)
                .HasForeignKey(d => d.approval_owner_user_id)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("training_company_requests_approval_owner_user_id_fkey");

            entity.HasOne(d => d.assigned_advisor_user).WithMany(p => p.training_company_requestassigned_advisor_users)
                .HasForeignKey(d => d.assigned_advisor_user_id)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("training_company_requests_assigned_advisor_user_id_fkey");

            entity.HasOne(d => d.reviewer_user).WithMany(p => p.training_company_requestreviewer_users)
                .HasForeignKey(d => d.reviewer_user_id)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("training_company_requests_reviewer_user_id_fkey");

            entity.HasOne(d => d.student_user).WithMany(p => p.training_company_requeststudent_users)
                .HasForeignKey(d => d.student_user_id)
                .HasConstraintName("training_company_requests_student_user_id_fkey");
        });

        modelBuilder.Entity<training_plan>(entity =>
        {
            entity.HasKey(e => e.id).HasName("training_plans_pkey");

            entity.Property(e => e.created_at).HasDefaultValueSql("now()");
            entity.Property(e => e.plan_title).HasMaxLength(250);
            entity.Property(e => e.submitted_at).HasDefaultValueSql("now()");
            entity.Property(e => e.updated_at).HasDefaultValueSql("now()");

            entity.HasOne(d => d.approval_owner_user).WithMany(p => p.training_planapproval_owner_users)
                .HasForeignKey(d => d.approval_owner_user_id)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("training_plans_approval_owner_user_id_fkey");

            entity.HasOne(d => d.assigned_advisor_user).WithMany(p => p.training_planassigned_advisor_users)
                .HasForeignKey(d => d.assigned_advisor_user_id)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("training_plans_assigned_advisor_user_id_fkey");

            entity.HasOne(d => d.internship).WithMany(p => p.training_plans)
                .HasForeignKey(d => d.internship_id)
                .HasConstraintName("training_plans_internship_id_fkey");

            entity.HasOne(d => d.reviewer_user).WithMany(p => p.training_planreviewer_users)
                .HasForeignKey(d => d.reviewer_user_id)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("training_plans_reviewer_user_id_fkey");

            entity.HasOne(d => d.student_user).WithMany(p => p.training_planstudent_users)
                .HasForeignKey(d => d.student_user_id)
                .HasConstraintName("training_plans_student_user_id_fkey");
        });

        modelBuilder.Entity<training_task>(entity =>
        {
            entity.HasKey(e => e.id).HasName("training_tasks_pkey");

            entity.Property(e => e.created_at).HasDefaultValueSql("now()");
            entity.Property(e => e.task_title).HasMaxLength(300);
            entity.Property(e => e.updated_at).HasDefaultValueSql("now()");

            entity.HasOne(d => d.internship).WithMany(p => p.training_tasks)
                .HasForeignKey(d => d.internship_id)
                .HasConstraintName("training_tasks_internship_id_fkey");

            entity.HasOne(d => d.student_user).WithMany(p => p.training_tasks)
                .HasForeignKey(d => d.student_user_id)
                .HasConstraintName("training_tasks_student_user_id_fkey");

            entity.HasOne(d => d.training_plan).WithMany(p => p.training_tasks)
                .HasForeignKey(d => d.training_plan_id)
                .HasConstraintName("training_tasks_training_plan_id_fkey");
        });

        modelBuilder.Entity<training_task_evidence>(entity =>
        {
            entity.HasKey(e => e.id).HasName("training_task_evidences_pkey");

            entity.Property(e => e.file_name).HasMaxLength(255);
            entity.Property(e => e.uploaded_at).HasDefaultValueSql("now()");

            entity.HasOne(d => d.task).WithMany(p => p.training_task_evidences)
                .HasForeignKey(d => d.task_id)
                .HasConstraintName("training_task_evidences_task_id_fkey");
        });

        modelBuilder.Entity<user>(entity =>
        {
            entity.HasKey(e => e.id).HasName("users_pkey");

            entity.Property(e => e.created_at).HasDefaultValueSql("now()");
            entity.Property(e => e.email).HasMaxLength(255);
            entity.Property(e => e.full_name).HasMaxLength(200);
            entity.Property(e => e.is_email_confirmed).HasDefaultValue(false);
            entity.Property(e => e.phone).HasMaxLength(50);
            entity.Property(e => e.updated_at).HasDefaultValueSql("now()");
            entity.Property(e => e.username).HasMaxLength(150);

            entity.HasOne(d => d.created_by_user).WithMany(p => p.Inversecreated_by_user)
                .HasForeignKey(d => d.created_by_user_id)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("users_created_by_user_id_fkey");
        });

        modelBuilder.Entity<user_role>(entity =>
        {
            entity.HasKey(e => e.id).HasName("user_roles_pkey");

            entity.HasIndex(e => new { e.user_id, e.role_id }, "user_roles_user_id_role_id_key").IsUnique();

            entity.Property(e => e.created_at).HasDefaultValueSql("now()");

            entity.HasOne(d => d.role).WithMany(p => p.user_roles)
                .HasForeignKey(d => d.role_id)
                .HasConstraintName("user_roles_role_id_fkey");

            entity.HasOne(d => d.user).WithMany(p => p.user_roles)
                .HasForeignKey(d => d.user_id)
                .HasConstraintName("user_roles_user_id_fkey");
        });

        modelBuilder.Entity<user_session>(entity =>
        {
            entity.HasKey(e => e.id).HasName("user_sessions_pkey");

            entity.Property(e => e.created_at).HasDefaultValueSql("now()");
            entity.Property(e => e.ip_address).HasMaxLength(100);

            entity.HasOne(d => d.user).WithMany(p => p.user_sessions)
                .HasForeignKey(d => d.user_id)
                .HasConstraintName("user_sessions_user_id_fkey");
        });

        modelBuilder.Entity<vw_attendance_summary>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_attendance_summary");

            entity.Property(e => e.internship_title).HasMaxLength(250);
            entity.Property(e => e.provider_name).HasMaxLength(250);
            entity.Property(e => e.student_name).HasMaxLength(200);
        });

        modelBuilder.Entity<vw_student_current_advisor>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_student_current_advisor");

            entity.Property(e => e.advisor_email).HasMaxLength(255);
            entity.Property(e => e.advisor_name).HasMaxLength(200);
            entity.Property(e => e.student_email).HasMaxLength(255);
            entity.Property(e => e.student_name).HasMaxLength(200);
        });

        modelBuilder.Entity<weekly_report>(entity =>
        {
            entity.HasKey(e => e.id).HasName("weekly_reports_pkey");

            entity.HasIndex(e => new { e.internship_id, e.week_no }, "weekly_reports_internship_id_week_no_key").IsUnique();

            entity.Property(e => e.created_at).HasDefaultValueSql("now()");
            entity.Property(e => e.evidence_count).HasDefaultValue(0);
            entity.Property(e => e.generated_at).HasDefaultValueSql("now()");
            entity.Property(e => e.generated_from_tasks).HasDefaultValue(true);
            entity.Property(e => e.report_title).HasMaxLength(250);
            entity.Property(e => e.total_tasks).HasDefaultValue(0);
            entity.Property(e => e.updated_at).HasDefaultValueSql("now()");

            entity.HasOne(d => d.approval_owner_user).WithMany(p => p.weekly_reportapproval_owner_users)
                .HasForeignKey(d => d.approval_owner_user_id)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("weekly_reports_approval_owner_user_id_fkey");

            entity.HasOne(d => d.assigned_advisor_user).WithMany(p => p.weekly_reportassigned_advisor_users)
                .HasForeignKey(d => d.assigned_advisor_user_id)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("weekly_reports_assigned_advisor_user_id_fkey");

            entity.HasOne(d => d.internship).WithMany(p => p.weekly_reports)
                .HasForeignKey(d => d.internship_id)
                .HasConstraintName("weekly_reports_internship_id_fkey");

            entity.HasOne(d => d.reviewer_user).WithMany(p => p.weekly_reportreviewer_users)
                .HasForeignKey(d => d.reviewer_user_id)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("weekly_reports_reviewer_user_id_fkey");

            entity.HasOne(d => d.student_user).WithMany(p => p.weekly_reportstudent_users)
                .HasForeignKey(d => d.student_user_id)
                .HasConstraintName("weekly_reports_student_user_id_fkey");

            entity.HasOne(d => d.training_plan).WithMany(p => p.weekly_reports)
                .HasForeignKey(d => d.training_plan_id)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("weekly_reports_training_plan_id_fkey");
        });

        modelBuilder.Entity<weekly_report_item>(entity =>
        {
            entity.HasKey(e => e.id).HasName("weekly_report_items_pkey");

            entity.HasIndex(e => new { e.weekly_report_id, e.task_id }, "weekly_report_items_weekly_report_id_task_id_key").IsUnique();

            entity.HasOne(d => d.task).WithMany(p => p.weekly_report_items)
                .HasForeignKey(d => d.task_id)
                .HasConstraintName("weekly_report_items_task_id_fkey");

            entity.HasOne(d => d.weekly_report).WithMany(p => p.weekly_report_items)
                .HasForeignKey(d => d.weekly_report_id)
                .HasConstraintName("weekly_report_items_weekly_report_id_fkey");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
