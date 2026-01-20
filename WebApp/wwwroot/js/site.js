// Connect 4 Game Drop Animation Controller
// Handles piece drop animations with proper timing and board state management

/**
 * Animation controller for Connect 4 game piece drops
 * Manages drop and bounce animations, board state, and accessibility
 *
 * IMPORTANT CHANGE:
 * - Color is now passed explicitly as 'blue' or 'red'
 * - No more guessing color from "human/ai"
 */
class Connect4AnimationController {
    constructor() {
        this.isAnimating = false;
        this.animationQueue = [];
        this.boardElement = null;
        this.columnButtons = [];

        // Initialize when DOM is ready
        if (document.readyState === 'loading') {
            document.addEventListener('DOMContentLoaded', () => this.initialize());
        } else {
            this.initialize();
        }
    }

    /**
     * Initialize the animation controller
     */
    initialize() {
        this.boardElement = document.getElementById('game-board');
        this.columnButtons = document.querySelectorAll('.column-btn');

        // Add event listeners to prevent interaction during animations
        this.setupBoardInteraction();
    }

    /**
     * Setup board interaction handlers
     */
    setupBoardInteraction() {
        if (this.boardElement) {
            this.boardElement.addEventListener('click', (e) => {
                if (this.isAnimating) {
                    e.preventDefault();
                    e.stopPropagation();
                    return false;
                }
            });
        }
    }

    /**
     * Animate a piece drop in the specified column and row
     * @param {number} column - Column index (0-6)
     * @param {number} row - Row index (0-5)
     * @param {string} color - 'blue' or 'red'
     * @returns {Promise<void>} Promise that resolves when animation completes
     */
    async animatePieceDrop(column, row, color = 'blue') {
        // If already animating, queue it
        if (this.isAnimating) {
            console.warn('Animation already in progress, queuing...');
            return this.queueAnimation(column, row, color);
        }

        if (!this.validateCoordinates(column, row)) {
            throw new Error(`Invalid coordinates: column=${column}, row=${row}`);
        }

        try {
            this.isAnimating = true;
            this.disableBoardInteraction();

            const cell = this.getCellElement(column, row);
            if (!cell) {
                throw new Error(`Cell not found at column=${column}, row=${row}`);
            }

            const pieceElement = this.createAnimatedPieceElement(cell, color);

            // distance based on row so tall boards animate correctly
            const cellH = cell.getBoundingClientRect().height;
            const gap = 8; // matches your table border-spacing
            const from = -((row + 1) * (cellH + gap) + 40);

            pieceElement.style.setProperty('--drop-from', `${from}px`);
            pieceElement.style.setProperty('--drop-duration', `1.5s`);

            await this.performDropAnimation(pieceElement);

            this.cleanupAnimation(pieceElement);
        } catch (error) {
            console.error('Animation error:', error);
            throw error;
        } finally {
            this.isAnimating = false;
            this.enableBoardInteraction();

            // Process any queued animations
            this.processAnimationQueue();
        }
    }

    /**
     * Queue an animation if one is already in progress
     * @param {number} column
     * @param {number} row
     * @param {string} color - 'blue' or 'red'
     * @returns {Promise<void>}
     */
    queueAnimation(column, row, color) {
        return new Promise((resolve, reject) => {
            this.animationQueue.push({
                column,
                row,
                color,
                resolve,
                reject
            });
        });
    }

    /**
     * Process queued animations
     */
    async processAnimationQueue() {
        if (this.animationQueue.length > 0 && !this.isAnimating) {
            const nextAnimation = this.animationQueue.shift();
            try {
                await this.animatePieceDrop(
                    nextAnimation.column,
                    nextAnimation.row,
                    nextAnimation.color
                );
                nextAnimation.resolve();
            } catch (error) {
                nextAnimation.reject(error);
            }
        }
    }

    /**
     * Validate column and row coordinates
     */
    validateCoordinates(column, row) {
        if (!Number.isInteger(column) || !Number.isInteger(row)) return false;
        if (column < 0 || row < 0) return false;
        if (!this.boardElement) return false;

        // boardElement is the <table id="game-board">
        const rowCount = this.boardElement.querySelectorAll('tr').length;
        if (rowCount === 0) return false;

        const colCount = this.boardElement.querySelectorAll('tr')[0].querySelectorAll('td').length;

        return column < colCount && row < rowCount;
    }

    /**
     * Get the cell element at specified coordinates
     */
    getCellElement(column, row) {
        if (!this.boardElement) return null;

        const selectors = [
            `tr:nth-child(${row + 1}) td:nth-child(${column + 1})`,
            `[data-row="${row}"][data-column="${column}"]`,
            `.game-cell[data-row="${row}"][data-column="${column}"]`
        ];

        for (const selector of selectors) {
            const cell = this.boardElement.querySelector(selector);
            if (cell) return cell;
        }

        return null;
    }

    /**
     * Create an animated piece element in the specified cell.
     * We remove any existing .dot to avoid "ghost + real" stacking.
     * @param {HTMLElement} cell
     * @param {string} color - 'blue' or 'red'
     * @returns {HTMLElement}
     */
    createAnimatedPieceElement(cell, color) {
        // Remove existing dot if any (prevents double/ghost)
        const existing = cell.querySelector('.dot');
        if (existing) existing.remove();

        
        const pieceElement = document.createElement('div');
        pieceElement.className = 'dot';

        const normalized = (String(color).toLowerCase() === 'red') ? 'red' : 'blue';
        pieceElement.classList.add(normalized);

        cell.appendChild(pieceElement);
        return pieceElement;
    }

    /**
     * Perform the drop animation
     */
    performDropAnimation(pieceElement) {
        return new Promise((resolve) => {
            pieceElement.classList.add('piece-dropping', 'piece-animating');

            const handleAnimationEnd = (e) => {
                if (e.animationName === 'dropPiece') {
                    pieceElement.removeEventListener('animationend', handleAnimationEnd);
                    resolve();
                }
            };

            pieceElement.addEventListener('animationend', handleAnimationEnd);

            const durStr = getComputedStyle(pieceElement).animationDuration || '0.6s';
            // animationDuration can be "1.5s" or "600ms"
            const ms =
                durStr.includes('ms')
                    ? parseFloat(durStr)
                    : parseFloat(durStr) * 1000;

            setTimeout(() => {
                pieceElement.removeEventListener('animationend', handleAnimationEnd);
                resolve();
            }, ms + 100); // small buffer
        });
    }

    
    /**
     * Clean up animation classes and states
     */
    cleanupAnimation(pieceElement) {
        pieceElement.classList.remove('piece-dropping', 'piece-bounce', 'piece-animating');
    }

    /**
     * Disable board interaction during animation
     */
    disableBoardInteraction() {
        if (this.boardElement) {
            this.boardElement.classList.add('board-animating');
        }

        this.columnButtons.forEach(btn => {
            btn.disabled = true;
            btn.classList.add('disabled');
        });
    }

    /**
     * Enable board interaction after animation
     */
    enableBoardInteraction() {
        if (this.boardElement) {
            this.boardElement.classList.remove('board-animating');
        }

        // IMPORTANT:
        // Don't blindly enable buttons if the game is already ended.
        // If your end-state handler disables them, keep them disabled.
        // We'll only re-enable ones that are NOT already disabled by game-end logic.
        this.columnButtons.forEach(btn => {
            // If button is disabled for game end, it will still have "disabled" class.
            // But during animation, we also add "disabled". We can't distinguish easily.
            // So: re-enable only if the page still has an active-game board class.
            const board = document.getElementById('game-board');
            const stillActive = board && board.classList.contains('active-game');

            if (stillActive) {
                btn.disabled = false;
                btn.classList.remove('disabled');
            }
        });
    }
}

// Global animation controller instance
window.connect4Animation = new Connect4AnimationController();

/**
 * Trigger a piece drop animation
 * @param {number} column
 * @param {number} row
 * @param {string} color - 'blue' or 'red'
 * @returns {Promise<void>}
 */
window.animatePieceDrop = function (column, row, color = 'blue') {
    return window.connect4Animation.animatePieceDrop(column, row, color);
};

// Export for module systems if available
if (typeof module !== 'undefined' && module.exports) {
    module.exports = { Connect4AnimationController };
}