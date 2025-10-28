pipeline {
    agent any

    environment {
        DOTNET_CLI_HOME = '/var/jenkins_home/.dotnet'
        DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
        DOTNET_NOLOGO = '1'
        PROJECT_PATH = 'GESCOMPH/WebGESCOMPH/WebGESCOMPH.csproj'
    }

    stages {

        // =======================================================
        // 1️⃣ CHECKOUT
        // =======================================================
        stage('Checkout código fuente') {
            steps {
                echo "📥 Clonando repositorio desde GitHub..."
                checkout scm
                sh 'ls -R GESCOMPH/DevOps || true'
            }
        }

        // =======================================================
        // 2️⃣ DETECTAR ENTORNO SEGÚN LA RAMA
        // =======================================================
        stage('Detectar entorno') {
            steps {
                script {
                    // Mapear ramas con entornos
                    switch (env.BRANCH_NAME) {
                        case 'main':
                            env.ENVIRONMENT = 'prod'
                            break
                        case 'staging':
                            env.ENVIRONMENT = 'staging'
                            break
                        case 'qa':
                            env.ENVIRONMENT = 'qa'
                            break
                        default:
                            env.ENVIRONMENT = 'develop'
                            break
                    }

                    env.ENV_DIR = "GESCOMPH/DevOps/${env.ENVIRONMENT}"
                    env.COMPOSE_FILE = "${env.ENV_DIR}/docker-compose.yml"
                    env.ENV_FILE = "${env.ENV_DIR}/.env"

                    echo """
                    ✅ Rama detectada: ${env.BRANCH_NAME}
                    🌎 Entorno asignado: ${env.ENVIRONMENT}
                    📄 Compose file: ${env.COMPOSE_FILE}
                    📁 Env file: ${env.ENV_FILE}
                    """

                    if (!fileExists(env.COMPOSE_FILE)) {
                        error "❌ No se encontró ${env.COMPOSE_FILE}"
                    }
                }
            }
        }

        // =======================================================
        // 3️⃣ COMPILAR Y PUBLICAR .NET
        // =======================================================
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

        // =======================================================
        // 4️⃣ CONSTRUIR IMAGEN DOCKER
        // =======================================================
        stage('Construir imagen Docker') {
            steps {
                dir('GESCOMPH') {
                    sh """
                        echo "🐳 Construyendo imagen Docker para GESCOMPH (${env.ENVIRONMENT})"
                        docker build -t gescomph-api-${env.ENVIRONMENT}:latest -f WebGESCOMPH/Dockerfile .
                    """
                }
            }
        }

        // =======================================================
        // 5️⃣ DESPLEGAR CON DOCKER COMPOSE
        // =======================================================
        stage('Desplegar GESCOMPH') {
            steps {
                dir('.') {
                    sh """
                        echo "🚀 Desplegando entorno: ${env.ENVIRONMENT}"
                        docker compose -f ${env.COMPOSE_FILE} --env-file ${env.ENV_FILE} up -d --build
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
        always {
            echo "🧹 Limpieza final del pipeline completada."
        }
    }
}
