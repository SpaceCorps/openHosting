#!/bin/bash

echo "Starting OpenHosting..."
echo ""
echo "Make sure Docker is running before starting the application."
echo ""
echo "The application will be available at: http://localhost:8080"
echo ""
read -p "Press Enter to continue..."

dotnet run
