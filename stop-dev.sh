#!/bin/bash

echo "🛑 Stopping Lab Specimen Management Backend..."
echo ""

# Stop Docker services
echo "📦 Stopping Docker services..."
docker compose down

echo ""
echo "✅ All services stopped"
echo ""
echo "To remove all data volumes, run:"
echo "   docker compose down -v"
