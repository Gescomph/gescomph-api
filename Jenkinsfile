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
                    switch (env.BRANCH_NAME) {
                        case 'main': env.ENVIRONMENT = 'prod'; break
                        case 'staging': env.ENVIRONMENT = 'staging'; break
                        case 'qa': env.ENVIRONMENT = 'qa'; break
                        default: env.ENVIRONMENT = 'develop'; break
                    }

                    env.ENV_DIR = "GESCOMPH/DevOps/${env.ENVIRONMENT}"
                    env.COMPOSE_FILE = "${env.ENV_DIR}/docker-compose.yml"
                    env.ENV_FILE = "${env.ENV_DIR}/.env"

                    // 📂 Ruta de docker-compose de bases de datos compartidas
                    env.DB_COMPOSE_FILE = "GESCOMPH-DB/docker-compose.yml"

                    echo """
                     Rama detectada: ${env.BRANCH_NAME}
                    🌎 Entorno asignado: ${env.ENVIRONMENT}
                    📄 Compose file (API): ${env.COMPOSE_FILE}
                    📁 Env file (API): ${env.ENV_FILE}
                    🗄️ Compose file (DB): ${env.DB_COMPOSE_FILE}
                    """

                    if (!fileExists(env.COMPOSE_FILE)) {
                        error " No se encontró ${env.COMPOSE_FILE}"
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
        // 5️⃣ PREPARAR RED Y BASES DE DATOS
        // =======================================================
        stage('Preparar red y base de datos') {
            steps {
                script {
                    sh """
                        echo "🌐 Creando red externa compartida (si no existe)..."
                        docker network create gescomph_network || true

                        echo "🗄️ Levantando stack de bases de datos..."
                        docker compose -f ${env.DB_COMPOSE_FILE} up -d
                    """
                }
            }
        }

        // =======================================================
        // 6️⃣ DESPLEGAR API CON DOCKER COMPOSE
        // =======================================================
        stage('Desplegar GESCOMPH API') {
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
