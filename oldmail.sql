CREATE TABLE `m_user` (
  `username` varchar(20) NOT NULL,
  `finished` tinyint(4) NOT NULL DEFAULT '0',
  `sort` int(4) DEFAULT NULL,
  PRIMARY KEY (`username`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;


CREATE TABLE `m_folder` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `username` varchar(20) NOT NULL,
  `folder_id` varchar(20) DEFAULT NULL,
  `folder_name` varchar(20) DEFAULT NULL,
  `finished` tinyint(4) NOT NULL DEFAULT '0',
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=678 DEFAULT CHARSET=utf8;


CREATE TABLE `m_mail` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `username` varchar(20) NOT NULL,
  `folder_id` varchar(20) DEFAULT NULL,
  `mail_id` varchar(50) DEFAULT NULL,
  `mail_subject` varchar(2000) DEFAULT NULL,
  `mail_from` varchar(2000) DEFAULT NULL,
  `mail_to` text,
  `mail_cc` text,
  `mail_bcc` varchar(2000) DEFAULT NULL,
  `mail_date` datetime DEFAULT NULL,
  `is_html` tinyint(4) NOT NULL DEFAULT '0',
  `mail_body` longtext,
  `attachments` text,
  `status` tinyint(4) NOT NULL DEFAULT '0',
  `search_text` longtext,
  PRIMARY KEY (`id`),
  KEY `idx_mail_id` (`mail_id`) USING BTREE,
  KEY `idx_username_maildate` (`username`,`mail_date`) USING BTREE
) ENGINE=InnoDB AUTO_INCREMENT=23433 DEFAULT CHARSET=utf8;
