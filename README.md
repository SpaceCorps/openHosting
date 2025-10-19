# OpenHosting - Docker Container Hosting Service

A simple, self-hosted Docker container management platform built with Ivy Framework that allows users to deploy and manage Docker containers through a web interface with GitHub integration.

## 🎯 Project Overview

OpenHosting is a container hosting service that provides:

- Web-based Docker container management
- GitHub repository integration with auto-deployment
- Real-time container status monitoring
- Port management and allocation
- Simple deployment workflow

## 🏗️ Architecture

### Core Components

1. **Ivy Web Application**
   - Main application built with Ivy Framework
   - Web interface for container management
   - REST API for container operations

2. **Docker Service**
   - Docker API integration for container lifecycle management
   - Image building from Dockerfiles
   - Container monitoring and health checks

3. **GitHub Integration**
   - Webhook listener for repository push events
   - Automatic image rebuilding on main branch updates
   - Repository cloning and Dockerfile detection

4. **Container Registry**
   - Local image storage and management
   - Version tracking for deployed containers

### Technology Stack

- **Backend**: Ivy Framework (.NET 9)
- **Container Runtime**: Docker
- **Database**: SQLite (for metadata storage)
- **Frontend**: Ivy widgets and components
- **Git Integration**: LibGit2Sharp
- **Webhooks**: ASP.NET Core webhook handling

## 📋 Features

### Phase 1: Core Functionality

- [x] Basic Ivy application setup
- [x] Docker container management
- [x] Web interface for container operations
- [x] Container status monitoring
- [x] Port allocation and management

### Phase 2: GitHub Integration

- [x] GitHub repository integration (basic)
- [x] Webhook setup for auto-deployment
- [x] Dockerfile detection and building (placeholder)
- [x] Automatic container recreation on push

### Phase 3: Advanced Features

- [ ] Container logs viewing
- [ ] Resource usage monitoring
- [ ] Multiple environment support
- [ ] User authentication and authorization
- [ ] Container networking management

## 🚀 Getting Started

### Prerequisites

- .NET 9 SDK
- Docker Desktop or Docker Engine
- Git (for repository cloning)

### Installation

1. Clone the repository
2. Run `dotnet restore`
3. Start Docker service
4. Run the application:
   - **Windows**: Double-click `start.bat` or run `dotnet run`
   - **Linux/Mac**: Run `./start.sh` or `dotnet run`

### Configuration

- The application will be available at `http://localhost:8080`
- Configure GitHub webhook secret in `appsettings.json`
- Set up port ranges for container allocation in configuration
- Ensure Docker daemon is accessible

### Docker Deployment

For production deployment using Docker:

```bash
# Build and run with Docker Compose
docker-compose up -d

# Or build and run manually
docker build -t openhosting .
docker run -p 8080:80 -v /var/run/docker.sock:/var/run/docker.sock openhosting
```

### GitHub Webhook Setup

1. Go to your GitHub repository settings
2. Navigate to "Webhooks" section
3. Add a new webhook with URL: `http://your-domain.com/api/webhook/github`
4. Set the secret to match your `appsettings.json` configuration
5. Select "Just the push event" for the trigger

## 📁 Project Structure

```
OpenHosting/
├── Apps/                    # Ivy application files
│   ├── HelloApp.cs         # Main application entry point
│   └── ContainerApp.cs     # Container management app
├── Services/               # Business logic services
│   ├── DockerService.cs    # Docker API integration
│   ├── GitHubService.cs    # GitHub webhook handling
│   └── ContainerService.cs # Container lifecycle management
├── Models/                 # Data models
│   ├── Container.cs        # Container entity
│   ├── Deployment.cs       # Deployment configuration
│   └── GitHubWebhook.cs    # Webhook payload models
├── Controllers/            # API controllers
│   └── ContainerController.cs
├── Views/                  # Ivy view components
│   ├── ContainerList.cs    # Container listing view
│   ├── DeploymentForm.cs   # Deployment creation form
│   └── StatusDashboard.cs  # Status monitoring dashboard
└── Program.cs              # Application entry point
```

## 🔧 API Endpoints

### Container Management

- `GET /api/containers` - List all containers
- `POST /api/containers` - Create new container
- `GET /api/containers/{id}` - Get container details
- `PUT /api/containers/{id}` - Update container
- `DELETE /api/containers/{id}` - Stop and remove container
- `POST /api/containers/{id}/start` - Start container
- `POST /api/containers/{id}/stop` - Stop container
- `GET /api/containers/{id}/logs` - Get container logs

### GitHub Integration

- `POST /api/webhooks/github` - GitHub webhook endpoint
- `POST /api/deployments` - Create deployment from GitHub repo
- `GET /api/deployments` - List all deployments

### System Information

- `GET /api/system/status` - System health status
- `GET /api/system/ports` - Available port information
- `GET /api/system/resources` - Resource usage statistics

## 🔐 Security Considerations

- GitHub webhook signature verification
- Docker daemon access control
- Port allocation security
- Container isolation
- Input validation and sanitization

## 🚀 Deployment Workflow

1. **Manual Deployment**
   - User provides GitHub repository URL
   - System clones repository and detects Dockerfile
   - Builds Docker image
   - Deploys container with allocated port
   - Updates status dashboard

2. **Automatic Deployment**
   - GitHub webhook triggers on main branch push
   - System identifies affected deployments
   - Rebuilds and redeploys containers
   - Notifies user of deployment status

## 📊 Monitoring and Logging

- Container health status tracking
- Resource usage monitoring (CPU, Memory, Network)
- Port usage tracking
- Deployment history and logs
- Error tracking and alerting

## 🔄 Future Enhancements

- Multi-node cluster support
- Load balancing and scaling
- Custom domain mapping
- SSL certificate management
- Container backup and restore
- Integration with external monitoring tools

## 📝 Development Notes

- Use Docker-in-Docker for testing
- Implement proper error handling and logging
- Add comprehensive unit and integration tests
- Follow Ivy Framework best practices
- Ensure responsive web interface design

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Submit a pull request

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.
