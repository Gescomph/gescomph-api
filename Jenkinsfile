pipeline {
    agent any

    environment {
        DOTNET_CLI_HOME = '/var/jenkins_home/.dotnet'
        DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
        DOTNET_NOLOGO = '1'
        PROJECT_PATH = 'GESCOMPH/WebGESCOMPH/WebGESCOMPH.csproj'
    }

    stages {

        stage('Detectar entorno desde GESCOMPH/.env') {
            steps {
                script {
                    def envValue = sh(
                        script: "grep '^ENVIRONMENT=' GESCOMPH/.env | cut -d '=' -f2 | tr -d '\\r\\n'",
                        returnStdout: true
                    ).trim()

                    if (!envValue) {
                        error "❌ No se encontró ENVIRONMENT en GESCOMPH/.env"
                    }

                    env.ENVIRONMENT = envValue
                    env.ENV_DIR = "DevOps/${env.ENVIRONMENT}"
                    env.COMPOSE_FILE = "${env.ENV_DIR}/docker-compose.yml"
                    env.ENV_FILE = "${env.ENV_DIR}/.env"

                    echo """
                    ✅ Entorno detectado: ${env.ENVIRONMENT}
                    📄 Archivo compose: ${env.COMPOSE_FILE}
                    📁 Archivo de entorno: ${env.ENV_FILE}
                    """
                }
            }
        }

        stage('Compilar .NET dentro de contenedor SDK') {
            steps {
                script {
                    docker.image('mcr.microsoft.com/dotnet/sdk:9.0')
                        .inside('-v /var/run/docker.sock:/var/run/docker.sock -u root:root') {
                        sh '''
                            echo "🔧 Restaurando dependencias .NET..."
                            cd GESCOMPH
                            dotnet restore WebGESCOMPH/WebGESCOMPH.csproj
                            dotnet build WebGESCOMPH/WebGESCOMPH.csproj --configuration Release
                            dotnet publish WebGESCOMPH/WebGESCOMPH.csproj -c Release -o ./publish
                        '''
                    }
                }
            }
        }

        stage('Construir imagen Docker') {
            steps {
                dir('GESCOMPH') {
                    sh """
                        echo "🐳 Construyendo imagen Docker para GESCOMPH (${env.ENVIRONMENT})"
                        docker build -t gescomph-${env.ENVIRONMENT}:latest -f WebGESCOMPH/Dockerfile .
                    """
                }
            }
        }

        stage('Desplegar GESCOMPH') {
            steps {
                dir('.') { // 🔁 ejecutar desde raíz del repo
                    sh """
                        echo "🚀 Desplegando GESCOMPH para entorno: ${env.ENVIRONMENT}"
                        docker compose -f GESCOMPH/${env.COMPOSE_FILE} --env-file GESCOMPH/${env.ENV_FILE} up -d --build
                    """
                }
            }
        }

    }

    post {
        success {
            echo "🎉 Despliegue completado correctamente para ${env.ENVIRONMENT}"
        }
        failure {
            echo "💥 Error durante el despliegue en ${env.ENVIRONMENT}"
        }
    }
}
