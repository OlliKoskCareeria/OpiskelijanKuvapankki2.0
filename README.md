# OpiskelijanKuvapankki2.0
# OpiskelijanKuvapankki2.0

## Overview

**OpiskelijanKuvapankki2.0** is a ASP.NET Core Web API backend for storing, managing, and serving images from a database. Application supports domain based access. 

This project is intended as a **backend portfolio project** and demonstrates how to build an API using .NET technologies.

---

## Features

### 🔐 Authentication & Authorization

* **ASP.NET Core Identity** for user management and secure password hashing
* **JWT (JSON Web Tokens)** for stateless authentication and authorization
* **Google reCAPTCHA v3** integrated into the user registration endpoint to prevent automated account creation

### 🖼️ Image Handling

* Image upload and retrieval through RESTful API endpoints
* Images are stored directly in the database
* **SixLabors.ImageSharp** is used for image validation and processing

### ✉️ Email Support

* Configurable email delivery with support for:

  * **SendGrid**
  * **Mailjet**
* Email provider can be selected via configuration

### 🚦 Security & Reliability

* **Rate limiting** to protect endpoints from abuse and brute-force attacks
* Centralized configuration for security-related settings
* Stateless API design suitable for scaling

---

## Tech Stack

* **ASP.NET Core Web API**
* **Entity Framework Core**
* **ASP.NET Core Identity**
* **JWT Authentication**
* **SixLabors.ImageSharp**
* **Google reCAPTCHA v3**
* **SendGrid / Mailjet**
* **Rate Limiting Middleware**


## Use Cases

* Backend for an image-based web or mobile application
* Secure API requiring authenticated users
* Portfolio example demonstrating backend development patterns

---



## Status

This project is actively developed as a backend-focused portfolio application.

---

## Author

Developed by **[Olli Koski]**

---

