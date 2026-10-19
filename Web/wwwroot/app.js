'use strict';
const $ = id => document.getElementById(id);
const examples = {
    swap: [[0, 2, 4, 6], [1, 1, 1, 3], [2, 2, 2, 6]],
    ordinary: [[2, 1, -1, 8], [-3, -1, 2, -11], [-2, 1, 2, -3]],
    dependent: [[0, 0, 2, 1, -1, 3, 4], [0, 1, 0, 2, 1, -1, 2], [0, 0, 0, 0, 2, 1, -2], [0, 1, 2, 3, 0, 2, 6], [0, 0, 4, 2, 0, 7, 6]]
};
let values = [], states = [], step = 0, timer = null, response = null, stepMode = true;

function message(text, error = false) {
    $('message').textContent = text;
    $('message').classList.toggle('error', error);
}

function stop() {
    clearInterval(timer);
    timer = null;
    $('play').textContent = '自动播放';
}

function invalidate() {
    stop();
    states = [];
    response = null;
    step = 0;
    $('result').hidden = true;
    $('empty').hidden = false;
    message('输入已更新，点击「开始演示」重新计算。');
}

function setMatrix(matrix) {
    values = matrix.map(row => row.map(String));
    invalidate();
    renderInput();
}

function renderInput() {
    const grid = $('input-grid');
    grid.replaceChildren();
    grid.style.gridTemplateColumns = `repeat(${values[0].length}, 66px)`;
    $('rows').value = values.length;
    $('columns').value = values[0].length;
    $('dimensions').textContent = `${values.length} × ${values[0].length}`;
    values.forEach((row, r) => row.forEach((value, c) => {
        const input = document.createElement('input');
        input.type = 'text';
        input.inputMode = 'decimal';
        input.value = value;
        input.setAttribute('aria-label', `第 ${r + 1} 行第 ${c + 1} 列`);
        input.addEventListener('input', () => {
            values[r][c] = input.value;
            input.removeAttribute('aria-invalid');
            invalidate();
        });
        input.addEventListener('paste', event => paste(event, r, c));
        grid.append(input);
    }));
}

function size() {
    const rows = Number($('rows').value), cols = Number($('columns').value);
    if (!Number.isInteger(rows) || !Number.isInteger(cols) || rows < 1 || cols < 1 || rows > 32 || cols > 32)
        throw new Error('行数、列数请输入 1～32 的整数。');
    return [rows, cols];
}

function resize() {
    try {
        const [rows, cols] = size();
        setMatrix(Array.from({length: rows}, (_, r) => Array.from({length: cols}, (_, c) => values[r]?.[c] ?? '0')));
    } catch (error) {
        message(error.message, true);
    }
}

function paste(event, startRow, startColumn) {
    const text = event.clipboardData.getData('text').trim();
    if (!/[\s,，]/.test(text)) return;
    event.preventDefault();
    const block = text.split(/\r?\n/).map(line => line.trim().split(/[\s,，]+/));
    if (block.some(row => row.length !== block[0].length || row.some(value => !isNumber(value)))) {
        message('粘贴失败：请使用等宽的数字矩阵，数字间以空格、逗号或 Tab 分隔。', true);
        return;
    }
    const rows = Math.max(values.length, startRow + block.length);
    const cols = Math.max(values[0].length, startColumn + block[0].length);
    if (rows > 32 || cols > 32) {
        message('粘贴后的矩阵不能超过 32×32。', true);
        return;
    }
    const matrix = Array.from({length: rows}, (_, r) => Array.from({length: cols}, (_, c) => values[r]?.[c] ?? '0'));
    block.forEach((row, r) => row.forEach((value, c) => matrix[startRow + r][startColumn + c] = value));
    setMatrix(matrix);
    message(`已粘贴 ${block.length}×${block[0].length} 个数字。`);
}

function isNumber(text) {
    return /^[+-]?(?:\d+\.?\d*|\.\d+)(?:[eE][+-]?\d+)?$/.test(text.trim()) && Number.isFinite(Number(text));
}

function readMatrix() {
    let invalid = null;
    const inputs = $('input-grid').querySelectorAll('input');
    const matrix = values.map((row, r) => row.map((value, c) => {
        const valid = value.trim() === '' || isNumber(value);
        inputs[r * row.length + c].setAttribute('aria-invalid', String(!valid));
        if (!valid && !invalid) invalid = inputs[r * row.length + c];
        return Number(value);
    }));
    if (invalid) {
        invalid.focus();
        throw new Error('请修正标红单元格，只能填写有限数字（例如 -2、0.5、1e-3）。');
    }
    return matrix;
}

async function calculate(steps) {
    stop();
    try {
        const matrix = readMatrix();
        if (steps && (matrix.length > 10 || matrix[0].length > 10))
            throw new Error('分步演示最多支持 10×10。较大矩阵请点击「直接完成」。');
        $('editor').disabled = true;
        $('result').hidden = true;
        $('empty').hidden = false;
        message('正在计算，请稍候…');
        const request = await fetch('/api/ref', {
            method: 'POST', headers: {'Content-Type': 'application/json'},
            body: JSON.stringify({matrix, steps})
        });
        const data = await request.json();
        if (!request.ok) throw new Error(data.error || '计算请求失败，请检查矩阵输入。');
        response = data;
        states = data.states;
        stepMode = steps;
        step = steps ? 0 : states.length - 1;
        $('empty').hidden = true;
        $('result').hidden = false;
        renderOutput();
        message(steps ? '计算完成。点击「下一步」或「自动播放」观察消元过程。' : '计算完成，当前显示最终结果。');
    } catch (error) {
        message(error instanceof TypeError ? '无法连接计算服务，请确认网站仍在运行。' : error.message, true);
    } finally {
        $('editor').disabled = false;
    }
}

function renderOutput() {
    if (!states.length) return;
    const matrix = states[step], previous = states[Math.max(0, step - 1)];
    const grid = $('output-grid');
    grid.replaceChildren();
    grid.style.gridTemplateColumns = `repeat(${matrix[0].length}, max-content)`;
    const precision = Number($('precision').value);
    matrix.forEach((row, r) => row.forEach((value, c) => {
        const cell = document.createElement('span');
        cell.className = 'cell';
        cell.textContent = value === 0 ? '0' : Math.abs(value) < 1e-10 ? '≈0' : Number(value.toPrecision(precision)).toString();
        cell.title = value.toString();
        cell.tabIndex = 0;
        cell.setAttribute('aria-label', `第 ${r + 1} 行第 ${c + 1} 列：${value}`);
        cell.classList.toggle('zero', value === 0);
        cell.classList.toggle('pivot-row', stepMode && step > 0 && r === step - 1);
        cell.classList.toggle('changed', step > 0 && value !== previous[r][c]);
        grid.append(cell);
    }));
    const last = step === states.length - 1;
    $('step-label').textContent = step === 0 ? '原矩阵 · 尚未处理' : stepMode ? `已处理 ${step} 个主元${last ? ' · 完成' : ''}` : '最终行阶梯形矩阵';
    $('time').textContent = `${response.elapsedMilliseconds.toFixed(2)} ms`;
    $('summary').textContent = `按当前算法的严格非零判断，共 ${response.pivotCount} 个主元。${stepMode ? '每一步包含该主元所需的换行及下方消元。' : '直接完成模式仅保留原矩阵和最终结果。'}`;
    $('step-slider').max = states.length - 1;
    $('step-slider').value = step;
    $('previous').disabled = step === 0;
    $('next').disabled = last;
    $('play').disabled = states.length < 2;
    $('reset').disabled = step === 0;
    if (last) stop();
}

$('resize').onclick = resize;
$('load').onclick = () => {
    setMatrix(examples[$('example').value]);
    message('示例已载入，可以直接开始演示。');
};
$('clear').onclick = () => setMatrix(values.map(row => row.map(() => 0)));
$('random').onclick = () => {
    try {
        const [rows, cols] = size();
        setMatrix(Array.from({length: rows}, () => Array.from({length: cols}, () => Math.floor(Math.random() * 19) - 9)));
    } catch (error) {
        message(error.message, true);
    }
};
$('calculate').onclick = () => calculate(true);
$('finish').onclick = () => calculate(false);
$('previous').onclick = () => {
    stop();
    step--;
    renderOutput();
};
$('next').onclick = () => {
    stop();
    step++;
    renderOutput();
};
$('reset').onclick = () => {
    stop();
    step = 0;
    renderOutput();
};
$('step-slider').oninput = () => {
    stop();
    step = Number($('step-slider').value);
    renderOutput();
};
$('precision').onchange = renderOutput;
$('play').onclick = () => {
    if (timer) {
        stop();
        return;
    }
    if (step === states.length - 1) {
        step = 0;
        renderOutput();
    }
    $('play').textContent = '暂停播放';
    timer = setInterval(() => {
        step++;
        renderOutput();
    }, 1100);
};
setMatrix(examples.swap);
message('已载入换行示例，可以直接开始演示。');
