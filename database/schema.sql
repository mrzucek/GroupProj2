-- ============================================
-- PhishGuard Database Schema
-- Anti-Phishing Cybersecurity Platform
-- ============================================

CREATE DATABASE IF NOT EXISTS phishguard;
USE phishguard;

-- ============================================
-- CORE TABLES
-- ============================================

-- Employees in the organization
CREATE TABLE employees (
    employee_id INT AUTO_INCREMENT PRIMARY KEY,
    email VARCHAR(255) NOT NULL UNIQUE,
    display_name VARCHAR(100) NOT NULL,
    department VARCHAR(100),
    role ENUM('employee', 'admin') NOT NULL DEFAULT 'employee',
    password_hash VARCHAR(255) NOT NULL,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    last_login DATETIME,
    is_active BOOLEAN NOT NULL DEFAULT TRUE
);

-- ============================================
-- EMAIL SCORING TABLES
-- ============================================

-- Incoming emails that have been analyzed
CREATE TABLE emails (
    email_id INT AUTO_INCREMENT PRIMARY KEY,
    recipient_id INT NOT NULL,
    sender_address VARCHAR(255) NOT NULL,
    sender_display_name VARCHAR(255),
    subject VARCHAR(500),
    body_preview VARCHAR(1000),
    received_at DATETIME NOT NULL,
    overall_score DECIMAL(5,2) NOT NULL DEFAULT 0,
    classification ENUM('safe', 'warning', 'blocked') NOT NULL DEFAULT 'safe',
    is_reported BOOLEAN NOT NULL DEFAULT FALSE,
    reported_at DATETIME,
    processed_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (recipient_id) REFERENCES employees(employee_id)
);

-- Individual scoring dimension results per email
CREATE TABLE email_scores (
    score_id INT AUTO_INCREMENT PRIMARY KEY,
    email_id INT NOT NULL,
    dimension ENUM(
        'threat_feed',        -- known blacklist/threat intelligence match
        'domain_reputation',  -- domain age, registration info, reputation
        'language_analysis',  -- urgency, pressure tactics, social engineering
        'behavioral_timing',  -- unusual sender patterns, timing anomalies
        'html_fingerprint',   -- email template structure comparison
        'sender_history'      -- historical pattern from internal data
    ) NOT NULL,
    score DECIMAL(5,2) NOT NULL DEFAULT 0,
    weight DECIMAL(3,2) NOT NULL DEFAULT 1.00,
    details VARCHAR(500),
    FOREIGN KEY (email_id) REFERENCES emails(email_id) ON DELETE CASCADE
);

-- URLs extracted from emails
CREATE TABLE email_urls (
    url_id INT AUTO_INCREMENT PRIMARY KEY,
    email_id INT NOT NULL,
    original_url VARCHAR(2000) NOT NULL,
    final_url VARCHAR(2000),
    domain VARCHAR(255),
    is_safe BOOLEAN,
    safe_browsing_result VARCHAR(100),
    domain_age_days INT,
    checked_at DATETIME,
    FOREIGN KEY (email_id) REFERENCES emails(email_id) ON DELETE CASCADE
);

-- ============================================
-- THREAT INTELLIGENCE TABLES
-- ============================================

-- Known malicious domains/senders from threat feeds
CREATE TABLE threat_indicators (
    indicator_id INT AUTO_INCREMENT PRIMARY KEY,
    indicator_type ENUM('domain', 'url', 'sender_email', 'ip_address', 'keyword_pattern') NOT NULL,
    indicator_value VARCHAR(500) NOT NULL,
    source VARCHAR(100) NOT NULL,
    severity ENUM('low', 'medium', 'high', 'critical') NOT NULL DEFAULT 'medium',
    first_seen DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    last_seen DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    INDEX idx_indicator_lookup (indicator_type, indicator_value)
);

-- Scoring rules and weights (configurable by admin)
CREATE TABLE scoring_rules (
    rule_id INT AUTO_INCREMENT PRIMARY KEY,
    dimension ENUM(
        'threat_feed',
        'domain_reputation',
        'language_analysis',
        'behavioral_timing',
        'html_fingerprint',
        'sender_history'
    ) NOT NULL,
    rule_name VARCHAR(100) NOT NULL,
    rule_description VARCHAR(500),
    pattern VARCHAR(500),
    score_value DECIMAL(5,2) NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_by INT,
    FOREIGN KEY (updated_by) REFERENCES employees(employee_id)
);

-- ============================================
-- EMPLOYEE REPORTING TABLES
-- ============================================

-- Employee phishing reports (feeds back into scoring)
CREATE TABLE phishing_reports (
    report_id INT AUTO_INCREMENT PRIMARY KEY,
    email_id INT NOT NULL,
    reported_by INT NOT NULL,
    report_reason VARCHAR(500),
    is_confirmed_phishing BOOLEAN,
    reviewed_by INT,
    reviewed_at DATETIME,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (email_id) REFERENCES emails(email_id),
    FOREIGN KEY (reported_by) REFERENCES employees(employee_id),
    FOREIGN KEY (reviewed_by) REFERENCES employees(employee_id)
);

-- ============================================
-- TRAINING & GAMIFICATION TABLES
-- ============================================

-- Simulated phishing campaign definitions
CREATE TABLE phishing_campaigns (
    campaign_id INT AUTO_INCREMENT PRIMARY KEY,
    campaign_name VARCHAR(200) NOT NULL,
    difficulty ENUM('easy', 'medium', 'hard', 'expert') NOT NULL,
    template_subject VARCHAR(500) NOT NULL,
    template_body TEXT NOT NULL,
    template_sender VARCHAR(255) NOT NULL,
    phishing_indicators VARCHAR(1000) NOT NULL,
    created_by INT NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (created_by) REFERENCES employees(employee_id)
);

-- Individual simulated phishing emails sent to employees
CREATE TABLE simulation_emails (
    simulation_id INT AUTO_INCREMENT PRIMARY KEY,
    campaign_id INT NOT NULL,
    target_employee_id INT NOT NULL,
    sent_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    opened_at DATETIME,
    clicked_link_at DATETIME,
    reported_at DATETIME,
    result ENUM('pending', 'no_action', 'clicked', 'reported') NOT NULL DEFAULT 'pending',
    FOREIGN KEY (campaign_id) REFERENCES phishing_campaigns(campaign_id),
    FOREIGN KEY (target_employee_id) REFERENCES employees(employee_id)
);

-- Employee training scores and difficulty tracking
CREATE TABLE employee_training (
    training_id INT AUTO_INCREMENT PRIMARY KEY,
    employee_id INT NOT NULL UNIQUE,
    current_difficulty ENUM('easy', 'medium', 'hard', 'expert') NOT NULL DEFAULT 'easy',
    total_simulations_received INT NOT NULL DEFAULT 0,
    total_correctly_reported INT NOT NULL DEFAULT 0,
    total_clicked INT NOT NULL DEFAULT 0,
    total_ignored INT NOT NULL DEFAULT 0,
    current_streak INT NOT NULL DEFAULT 0,
    best_streak INT NOT NULL DEFAULT 0,
    score_points INT NOT NULL DEFAULT 0,
    last_simulation_at DATETIME,
    difficulty_updated_at DATETIME,
    FOREIGN KEY (employee_id) REFERENCES employees(employee_id)
);

-- Leaderboard history (snapshot monthly for trends)
CREATE TABLE leaderboard_history (
    history_id INT AUTO_INCREMENT PRIMARY KEY,
    employee_id INT NOT NULL,
    period_month DATE NOT NULL,
    points_earned INT NOT NULL DEFAULT 0,
    simulations_passed INT NOT NULL DEFAULT 0,
    real_phishing_reported INT NOT NULL DEFAULT 0,
    rank_position INT,
    FOREIGN KEY (employee_id) REFERENCES employees(employee_id),
    UNIQUE KEY unique_employee_month (employee_id, period_month)
);

-- ============================================
-- ADMIN DASHBOARD / METRICS TABLES
-- ============================================

-- Daily aggregated security metrics
CREATE TABLE daily_metrics (
    metric_id INT AUTO_INCREMENT PRIMARY KEY,
    metric_date DATE NOT NULL UNIQUE,
    total_emails_scanned INT NOT NULL DEFAULT 0,
    total_blocked INT NOT NULL DEFAULT 0,
    total_warnings INT NOT NULL DEFAULT 0,
    total_safe INT NOT NULL DEFAULT 0,
    total_employee_reports INT NOT NULL DEFAULT 0,
    total_confirmed_phishing INT NOT NULL DEFAULT 0,
    total_urls_checked INT NOT NULL DEFAULT 0,
    total_urls_blocked INT NOT NULL DEFAULT 0,
    total_simulations_sent INT NOT NULL DEFAULT 0,
    total_simulations_caught INT NOT NULL DEFAULT 0,
    company_security_score DECIMAL(5,2)
);

-- Audit log for admin actions
CREATE TABLE audit_log (
    log_id INT AUTO_INCREMENT PRIMARY KEY,
    employee_id INT NOT NULL,
    action VARCHAR(100) NOT NULL,
    details VARCHAR(1000),
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (employee_id) REFERENCES employees(employee_id)
);

-- ============================================
-- INDEXES FOR PERFORMANCE
-- ============================================

CREATE INDEX idx_emails_recipient ON emails(recipient_id, received_at);
CREATE INDEX idx_emails_classification ON emails(classification);
CREATE INDEX idx_emails_score ON emails(overall_score);
CREATE INDEX idx_email_scores_dimension ON email_scores(email_id, dimension);
CREATE INDEX idx_email_urls_domain ON email_urls(domain);
CREATE INDEX idx_simulation_target ON simulation_emails(target_employee_id, result);
CREATE INDEX idx_daily_metrics_date ON daily_metrics(metric_date);
CREATE INDEX idx_phishing_reports_email ON phishing_reports(email_id);
