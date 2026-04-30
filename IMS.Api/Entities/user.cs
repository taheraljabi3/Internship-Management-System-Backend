using System;
using System.Collections.Generic;

namespace IMS.Api.Entities;

public partial class user
{
    public long id { get; set; }

    public string full_name { get; set; } = null!;

    public string email { get; set; } = null!;

    public string? username { get; set; }

    public string? phone { get; set; }

    public string? password_hash { get; set; }

    public bool is_email_confirmed { get; set; }

    public DateTime? last_login_at { get; set; }

    public DateTime created_at { get; set; }

    public DateTime updated_at { get; set; }

    public long? created_by_user_id { get; set; }

    public virtual ICollection<user> Inversecreated_by_user { get; set; } = new List<user>();

    public virtual academic_advisor_profile? academic_advisor_profile { get; set; }

    public virtual ICollection<academic_evaluation> academic_evaluationevaluator_users { get; set; } = new List<academic_evaluation>();

    public virtual ICollection<academic_evaluation> academic_evaluationstudent_users { get; set; } = new List<academic_evaluation>();

    public virtual administrator_profile? administrator_profile { get; set; }

    public virtual ICollection<advisor_student_assignment> advisor_student_assignmentadvisor_users { get; set; } = new List<advisor_student_assignment>();

    public virtual ICollection<advisor_student_assignment> advisor_student_assignmentassigned_by_users { get; set; } = new List<advisor_student_assignment>();

    public virtual advisor_student_assignment? advisor_student_assignmentstudent_user { get; set; }

    public virtual ICollection<approval_action> approval_actionactor_users { get; set; } = new List<approval_action>();

    public virtual ICollection<approval_action> approval_actionstudent_users { get; set; } = new List<approval_action>();

    public virtual ICollection<approval_delegation_log> approval_delegation_logchanged_by_users { get; set; } = new List<approval_delegation_log>();

    public virtual ICollection<approval_delegation_log> approval_delegation_logfrom_owner_users { get; set; } = new List<approval_delegation_log>();

    public virtual ICollection<approval_delegation_log> approval_delegation_logstudent_users { get; set; } = new List<approval_delegation_log>();

    public virtual ICollection<approval_delegation_log> approval_delegation_logto_owner_users { get; set; } = new List<approval_delegation_log>();

    public virtual ICollection<archived_record> archived_records { get; set; } = new List<archived_record>();

    public virtual ICollection<attendance_entry> attendance_entrycreated_by_users { get; set; } = new List<attendance_entry>();

    public virtual ICollection<attendance_entry> attendance_entrystudent_users { get; set; } = new List<attendance_entry>();

    public virtual ICollection<audit_log> audit_logs { get; set; } = new List<audit_log>();

    public virtual ICollection<company_evaluation_template> company_evaluation_templatecreated_by_users { get; set; } = new List<company_evaluation_template>();

    public virtual ICollection<company_evaluation_template> company_evaluation_templatestudent_users { get; set; } = new List<company_evaluation_template>();

    public virtual ICollection<company_evaluation> company_evaluations { get; set; } = new List<company_evaluation>();

    public virtual user? created_by_user { get; set; }

    public virtual ICollection<field_visit> field_visits { get; set; } = new List<field_visit>();

    public virtual ICollection<final_evaluation_request> final_evaluation_requestrequested_by_users { get; set; } = new List<final_evaluation_request>();

    public virtual ICollection<final_evaluation_request> final_evaluation_requeststudent_users { get; set; } = new List<final_evaluation_request>();

    public virtual internship? internship { get; set; }

    public virtual ICollection<invitation_batch> invitation_batchadvisor_users { get; set; } = new List<invitation_batch>();

    public virtual ICollection<invitation_batch> invitation_batchcreated_by_users { get; set; } = new List<invitation_batch>();

    public virtual ICollection<invitation_recipient> invitation_recipientadvisor_users { get; set; } = new List<invitation_recipient>();

    public virtual ICollection<invitation_recipient> invitation_recipientstudent_users { get; set; } = new List<invitation_recipient>();

    public virtual ICollection<notification> notifications { get; set; } = new List<notification>();

    public virtual ICollection<online_follow_up> online_follow_ups { get; set; } = new List<online_follow_up>();

    public virtual ICollection<student_course> student_courses { get; set; } = new List<student_course>();

    public virtual ICollection<student_document> student_documentgenerated_by_users { get; set; } = new List<student_document>();

    public virtual ICollection<student_document> student_documentstudent_users { get; set; } = new List<student_document>();

    public virtual ICollection<student_eligibility_review> student_eligibility_reviewapproval_owner_users { get; set; } = new List<student_eligibility_review>();

    public virtual ICollection<student_eligibility_review> student_eligibility_reviewreviewer_users { get; set; } = new List<student_eligibility_review>();

    public virtual student_eligibility_review? student_eligibility_reviewstudent_user { get; set; }

    public virtual student_profile? student_profile { get; set; }

    public virtual ICollection<student_project> student_projects { get; set; } = new List<student_project>();

    public virtual ICollection<student_skill> student_skills { get; set; } = new List<student_skill>();

    public virtual ICollection<training_company_request> training_company_requestapproval_owner_users { get; set; } = new List<training_company_request>();

    public virtual ICollection<training_company_request> training_company_requestassigned_advisor_users { get; set; } = new List<training_company_request>();

    public virtual ICollection<training_company_request> training_company_requestreviewer_users { get; set; } = new List<training_company_request>();

    public virtual ICollection<training_company_request> training_company_requeststudent_users { get; set; } = new List<training_company_request>();

    public virtual ICollection<training_plan> training_planapproval_owner_users { get; set; } = new List<training_plan>();

    public virtual ICollection<training_plan> training_planassigned_advisor_users { get; set; } = new List<training_plan>();

    public virtual ICollection<training_plan> training_planreviewer_users { get; set; } = new List<training_plan>();

    public virtual ICollection<training_plan> training_planstudent_users { get; set; } = new List<training_plan>();

    public virtual ICollection<training_task> training_tasks { get; set; } = new List<training_task>();

    public virtual ICollection<user_role> user_roles { get; set; } = new List<user_role>();

    public virtual ICollection<user_session> user_sessions { get; set; } = new List<user_session>();

    public virtual ICollection<weekly_report> weekly_reportapproval_owner_users { get; set; } = new List<weekly_report>();

    public virtual ICollection<weekly_report> weekly_reportassigned_advisor_users { get; set; } = new List<weekly_report>();

    public virtual ICollection<weekly_report> weekly_reportreviewer_users { get; set; } = new List<weekly_report>();

    public virtual ICollection<weekly_report> weekly_reportstudent_users { get; set; } = new List<weekly_report>();
}
